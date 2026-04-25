using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Homeless.Application.DTOs;
using Homeless.Application.Interfaces;

namespace Homeless.Infrastructure.Messaging;

public sealed class InMemoryPaymentEventStream : IPaymentEventStream
{
    private static readonly TimeSpan ReplayWindow = TimeSpan.FromSeconds(15);

    private readonly Lock _sync = new();
    private readonly Dictionary<long, Channel<PaymentEventResponse>> _subscribers = [];
    private readonly Queue<PaymentEventResponse> _recentEvents = new();
    private long _nextSubscriberId;

    public async ValueTask PublishAsync(
        PaymentEventResponse paymentEvent,
        CancellationToken cancellationToken = default
    )
    {
        Channel<PaymentEventResponse>[] subscribers;

        lock (_sync)
        {
            _recentEvents.Enqueue(paymentEvent);
            TrimExpiredEvents(paymentEvent.OccurredAt);
            subscribers = _subscribers.Values.ToArray();
        }

        foreach (var subscriber in subscribers)
            await subscriber
                .Writer.WriteAsync(paymentEvent, cancellationToken)
                .ConfigureAwait(false);
    }

    public async IAsyncEnumerable<PaymentEventResponse> ReadAllAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        var subscriber = Channel.CreateUnbounded<PaymentEventResponse>(
            new UnboundedChannelOptions { SingleReader = true, SingleWriter = false }
        );

        var subscriberId = Interlocked.Increment(ref _nextSubscriberId);
        PaymentEventResponse[] replay;

        lock (_sync)
        {
            _subscribers[subscriberId] = subscriber;
            TrimExpiredEvents(DateTime.UtcNow);
            replay = _recentEvents.ToArray();
        }

        try
        {
            foreach (var paymentEvent in replay)
                yield return paymentEvent;

            await foreach (
                var paymentEvent in subscriber
                    .Reader.ReadAllAsync(cancellationToken)
                    .ConfigureAwait(false)
            )
                yield return paymentEvent;
        }
        finally
        {
            lock (_sync)
                _subscribers.Remove(subscriberId);

            subscriber.Writer.TryComplete();
        }
    }

    private void TrimExpiredEvents(DateTime referenceTime)
    {
        while (
            _recentEvents.Count > 0
            && referenceTime - _recentEvents.Peek().OccurredAt > ReplayWindow
        )
            _recentEvents.Dequeue();
    }
}
