using System.Collections.Concurrent;
using System.Threading.Channels;
using Homeless.Application.DTOs;
using Homeless.Application.Interfaces;

namespace Homeless.Infrastructure.Messaging;

/// <summary>
/// In-memory pub/sub for image-processed SSE notifications.
/// One unbounded channel per property ID; multiple SSE clients
/// can read in parallel (SingleReader = false).
/// </summary>
public sealed class InMemoryImageProcessedStream : IImageProcessedStream
{
    private readonly ConcurrentDictionary<Guid, Channel<ImageProcessedEvent>> _channels = new();

    public async ValueTask PublishAsync(
        ImageProcessedEvent processedEvent,
        CancellationToken cancellationToken = default)
    {
        var channel = GetOrCreateChannel(processedEvent.PropertyId);
        await channel.Writer.WriteAsync(processedEvent, cancellationToken).ConfigureAwait(false);
    }

    public IAsyncEnumerable<ImageProcessedEvent> ReadAllAsync(
        Guid propertyId,
        CancellationToken cancellationToken = default)
    {
        var channel = GetOrCreateChannel(propertyId);
        return channel.Reader.ReadAllAsync(cancellationToken);
    }

    private Channel<ImageProcessedEvent> GetOrCreateChannel(Guid propertyId) =>
        _channels.GetOrAdd(propertyId, _ =>
            Channel.CreateUnbounded<ImageProcessedEvent>(new UnboundedChannelOptions
            {
                SingleReader = false,
                SingleWriter = false
            }));
}
