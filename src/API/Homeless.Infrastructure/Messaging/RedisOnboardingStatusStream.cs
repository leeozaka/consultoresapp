using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Homeless.Application.Interfaces;
using Homeless.Application.UseCases.Onboarding;

namespace Homeless.Infrastructure.Messaging;

public sealed class RedisOnboardingStatusStream : IOnboardingStatusStream, IDisposable
{
    private const string RedisChannelName = "sse:onboarding";
    private static readonly TimeSpan ReplayWindow = TimeSpan.FromSeconds(30);

    private readonly ISubscriber _subscriber;
    private readonly ILogger<RedisOnboardingStatusStream> _logger;

    private readonly Lock _sync = new();
    // keyed by (tenantId, subscriberId)
    private readonly Dictionary<(Guid, long), Channel<OnboardingStatusEvent>> _local = [];
    private readonly Dictionary<Guid, Queue<OnboardingStatusEvent>> _recentByTenant = [];
    private long _nextId;

    public RedisOnboardingStatusStream(
        IConnectionMultiplexer multiplexer,
        ILogger<RedisOnboardingStatusStream> logger)
    {
        _logger = logger;
        _subscriber = multiplexer.GetSubscriber();
        _subscriber.Subscribe(
            RedisChannel.Literal(RedisChannelName),
            OnRedisMessage);
    }

    public async ValueTask PublishAsync(
        OnboardingStatusEvent statusEvent,
        CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(statusEvent);
        await _subscriber.PublishAsync(
            RedisChannel.Literal(RedisChannelName),
            json).ConfigureAwait(false);
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

        var id = Interlocked.Increment(ref _nextId);
        OnboardingStatusEvent[] replay;

        lock (_sync)
        {
            _local[(tenantId, id)] = channel;

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
                _local.Remove((tenantId, id));

            channel.Writer.TryComplete();
        }
    }

    private void OnRedisMessage(RedisChannel _, RedisValue message)
    {
        OnboardingStatusEvent? statusEvent;
        try
        {
            statusEvent = JsonSerializer.Deserialize<OnboardingStatusEvent>(message.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize onboarding status event from Redis");
            return;
        }

        if (statusEvent is null)
            return;

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

            subscribers = _local
                .Where(kv => kv.Key.Item1 == statusEvent.TenantId)
                .Select(kv => kv.Value)
                .ToArray();
        }

        foreach (var sub in subscribers)
        {
            if (!sub.Writer.TryWrite(statusEvent))
                _logger.LogWarning(
                    "Onboarding SSE subscriber channel for tenant {TenantId} is full or completed, dropping event",
                    statusEvent.TenantId);
        }
    }

    private static void TrimExpiredEvents(Queue<OnboardingStatusEvent> queue, DateTime referenceTime)
    {
        while (queue.Count > 0 && referenceTime - queue.Peek().OccurredAt > ReplayWindow)
            queue.Dequeue();
    }

    public void Dispose() => _subscriber.UnsubscribeAll();
}
