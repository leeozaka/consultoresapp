using System.Text.Json;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Homeless.Application.DTOs;
using Homeless.Application.Interfaces;

namespace Homeless.Infrastructure.Messaging;

/// <summary>
/// Redis Pub/Sub-backed image-processed SSE stream.
/// Uses a single pattern subscription <c>sse:images:*</c> so that events published
/// on any replica (e.g. the Worker processing images) are fanned out to all API
/// replicas that have SSE clients waiting on that property.
/// </summary>
public sealed class RedisImageProcessedStream : IImageProcessedStream, IDisposable
{
    private const string ChannelPattern = "sse:images:*";
    private const string ChannelPrefix = "sse:images:";

    private readonly ISubscriber _subscriber;
    private readonly ILogger<RedisImageProcessedStream> _logger;

    private readonly Lock _sync = new();
    private readonly Dictionary<Guid, List<Channel<ImageProcessedEvent>>> _local = [];

    public RedisImageProcessedStream(
        IConnectionMultiplexer multiplexer,
        ILogger<RedisImageProcessedStream> logger)
    {
        _logger = logger;
        _subscriber = multiplexer.GetSubscriber();
        _subscriber.Subscribe(
            new RedisChannel(ChannelPattern, RedisChannel.PatternMode.Pattern),
            OnRedisMessage);
    }

    public async ValueTask PublishAsync(
        ImageProcessedEvent processedEvent,
        CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(processedEvent);
        await _subscriber.PublishAsync(
            RedisChannel.Literal($"{ChannelPrefix}{processedEvent.PropertyId}"),
            json).ConfigureAwait(false);
    }

    public async IAsyncEnumerable<ImageProcessedEvent> ReadAllAsync(
        Guid propertyId,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var channel = Channel.CreateUnbounded<ImageProcessedEvent>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        lock (_sync)
        {
            if (!_local.TryGetValue(propertyId, out var list))
            {
                list = [];
                _local[propertyId] = list;
            }
            list.Add(channel);
        }

        try
        {
            await foreach (var ev in channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
                yield return ev;
        }
        finally
        {
            lock (_sync)
            {
                if (_local.TryGetValue(propertyId, out var list))
                {
                    list.Remove(channel);
                    if (list.Count == 0)
                        _local.Remove(propertyId);
                }
            }

            channel.Writer.TryComplete();
        }
    }

    private void OnRedisMessage(RedisChannel redisChannel, RedisValue message)
    {
        var channelName = redisChannel.ToString();
        if (!channelName.StartsWith(ChannelPrefix, StringComparison.Ordinal))
            return;

        if (!Guid.TryParse(channelName[ChannelPrefix.Length..], out var propertyId))
        {
            _logger.LogWarning("Unexpected Redis image stream channel name: {Channel}", channelName);
            return;
        }

        ImageProcessedEvent? ev;
        try
        {
            ev = JsonSerializer.Deserialize<ImageProcessedEvent>(message.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize image event from Redis for property {PropertyId}", propertyId);
            return;
        }

        if (ev is null)
            return;

        Channel<ImageProcessedEvent>[] subscribers;
        lock (_sync)
            subscribers = _local.TryGetValue(propertyId, out var list) ? list.ToArray() : [];

        foreach (var sub in subscribers)
        {
            if (!sub.Writer.TryWrite(ev))
                _logger.LogWarning("Image SSE subscriber channel full or completed for property {PropertyId}", propertyId);
        }
    }

    public void Dispose() => _subscriber.UnsubscribeAll();
}
