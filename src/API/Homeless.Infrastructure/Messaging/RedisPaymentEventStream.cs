using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Homeless.Application.DTOs;
using Homeless.Application.Interfaces;

namespace Homeless.Infrastructure.Messaging;

/// <summary>
/// Redis Pub/Sub-backed payment SSE stream.
/// Events published on any replica are fanned out to all replicas via Redis,
/// ensuring every connected SSE client receives real-time notifications regardless
/// of which replica handled the Stripe webhook.
/// </summary>
public sealed class RedisPaymentEventStream : IPaymentEventStream, IDisposable
{
    private const string RedisChannelName = "sse:payments";
    private static readonly TimeSpan ReplayWindow = TimeSpan.FromSeconds(15);

    private readonly ISubscriber _subscriber;
    private readonly ILogger<RedisPaymentEventStream> _logger;

    private readonly Lock _sync = new();
    private readonly Dictionary<long, Channel<PaymentEventResponse>> _local = [];
    private readonly Queue<PaymentEventResponse> _recentEvents = new();
    private long _nextId;

    public RedisPaymentEventStream(
        IConnectionMultiplexer multiplexer,
        ILogger<RedisPaymentEventStream> logger)
    {
        _logger = logger;
        _subscriber = multiplexer.GetSubscriber();
        _subscriber.Subscribe(
            RedisChannel.Literal(RedisChannelName),
            OnRedisMessage);
    }

    public async ValueTask PublishAsync(
        PaymentEventResponse paymentEvent,
        CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(paymentEvent);
        await _subscriber.PublishAsync(
            RedisChannel.Literal(RedisChannelName),
            json).ConfigureAwait(false);
    }

    public async IAsyncEnumerable<PaymentEventResponse> ReadAllAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var channel = Channel.CreateUnbounded<PaymentEventResponse>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        var id = Interlocked.Increment(ref _nextId);
        PaymentEventResponse[] replay;

        lock (_sync)
        {
            _local[id] = channel;
            TrimExpiredEvents(DateTime.UtcNow);
            replay = _recentEvents.ToArray();
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
                _local.Remove(id);

            channel.Writer.TryComplete();
        }
    }

    private void OnRedisMessage(RedisChannel _, RedisValue message)
    {
        PaymentEventResponse? paymentEvent;
        try
        {
            paymentEvent = JsonSerializer.Deserialize<PaymentEventResponse>(message.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize payment event from Redis");
            return;
        }

        if (paymentEvent is null)
            return;

        Channel<PaymentEventResponse>[] subscribers;

        lock (_sync)
        {
            _recentEvents.Enqueue(paymentEvent);
            TrimExpiredEvents(paymentEvent.OccurredAt);
            subscribers = _local.Values.ToArray();
        }

        foreach (var sub in subscribers)
        {
            if (!sub.Writer.TryWrite(paymentEvent))
                _logger.LogWarning("Payment SSE subscriber channel is full or completed, dropping event");
        }
    }

    private void TrimExpiredEvents(DateTime referenceTime)
    {
        while (_recentEvents.Count > 0 && referenceTime - _recentEvents.Peek().OccurredAt > ReplayWindow)
            _recentEvents.Dequeue();
    }

    public void Dispose() => _subscriber.UnsubscribeAll();
}
