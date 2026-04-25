using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Homeless.Application.Interfaces;
using Homeless.Application.UseCases.Onboarding;

namespace Homeless.Infrastructure.Messaging;

public sealed class InMemoryOnboardingStatusStream : IOnboardingStatusStream
{
    private static readonly TimeSpan ReplayWindow = TimeSpan.FromSeconds(30);

    private readonly Lock _sync = new();
    // keyed by (tenantId, subscriberId)
    private readonly Dictionary<(Guid, long), Channel<OnboardingStatusEvent>> _subscribers = [];
    private readonly Dictionary<Guid, Queue<OnboardingStatusEvent>> _recentByTenant = [];
    private long _nextSubscriberId;

    public async ValueTask PublishAsync(
        OnboardingStatusEvent statusEvent,
        CancellationToken cancellationToken = default)
    {
        Channel<OnboardingStatusEvent>[] subscribers;

        lock (_sync)
        {
            if (!_recentByTenant.TryGetValue(statusEvent.TenantId, out var queue))
            {
                queue = new Queue<OnboardingStatusEvent>();
                _recentByTenant[statusEvent.TenantId] = queue;
            }

            queue.Enqueue(statusEvent);
            TrimExpiredEvents(queue, statusEvent.OccurredAt);

            subscribers = _subscribers
                .Where(kv => kv.Key.Item1 == statusEvent.TenantId)
                .Select(kv => kv.Value)
                .ToArray();
        }

        foreach (var subscriber in subscribers)
            await subscriber.Writer.WriteAsync(statusEvent, cancellationToken).ConfigureAwait(false);
    }

    public async IAsyncEnumerable<OnboardingStatusEvent> ReadForTenantAsync(
        Guid tenantId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var channel = Channel.CreateUnbounded<OnboardingStatusEvent>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        var subscriberId = Interlocked.Increment(ref _nextSubscriberId);
        OnboardingStatusEvent[] replay;

        lock (_sync)
        {
            _subscribers[(tenantId, subscriberId)] = channel;

            if (_recentByTenant.TryGetValue(tenantId, out var queue))
            {
                TrimExpiredEvents(queue, DateTime.UtcNow);
                replay = queue.ToArray();
            }
            else
            {
                replay = [];
            }
        }

        try
        {
            foreach (var ev in replay)
                yield return ev;

            await foreach (var ev in channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
                yield return ev;
        }
        finally
        {
            lock (_sync)
                _subscribers.Remove((tenantId, subscriberId));

            channel.Writer.TryComplete();
        }
    }

    private static void TrimExpiredEvents(Queue<OnboardingStatusEvent> queue, DateTime referenceTime)
    {
        while (queue.Count > 0 && referenceTime - queue.Peek().OccurredAt > ReplayWindow)
            queue.Dequeue();
    }
}
