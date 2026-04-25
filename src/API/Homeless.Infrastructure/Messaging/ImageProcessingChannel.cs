using System.Threading.Channels;
using Microsoft.Extensions.Options;
using Homeless.Application.Interfaces;

namespace Homeless.Infrastructure.Messaging;

/// <summary>
/// Bounded, single-producer/multi-consumer channel for async image processing.
/// Registered as Singleton so it outlives individual requests.
/// </summary>
public sealed class ImageProcessingChannel : IImageProcessingChannel
{
    private readonly Channel<ImageProcessingMessage> _channel;

    public ImageProcessingChannel(IOptions<ImageProcessingOptions> options)
    {
        var capacity = options.Value.ChannelCapacity;
        _channel = Channel.CreateBounded<ImageProcessingMessage>(
            new BoundedChannelOptions(capacity)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = false,
                SingleWriter = false
            });
    }

    public ValueTask WriteAsync(
        ImageProcessingMessage message,
        CancellationToken cancellationToken = default) =>
        _channel.Writer.WriteAsync(message, cancellationToken);

    public IAsyncEnumerable<ImageProcessingMessage> ReadAllAsync(
        CancellationToken cancellationToken = default) =>
        _channel.Reader.ReadAllAsync(cancellationToken);
}

public sealed class ImageProcessingOptions
{
    public const string SectionName = "ImageProcessing";

    public long MaxFileSizeBytes { get; set; } = 20_971_520; // 20 MiB
    public int WebPQuality { get; set; } = 90;
    public int ChannelCapacity { get; set; } = 100;
    public int ThumbnailWidth { get; set; } = 400;
    public int MediumWidth { get; set; } = 900;
    public int FullWidth { get; set; } = 1600;
}
