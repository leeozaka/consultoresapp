namespace Homeless.Application.Interfaces;

/// <summary>
/// Channel-based message for the background image processing pipeline.
/// Published by UploadPropertyImageHandler, consumed by ImageProcessingBackgroundService.
/// See roadmap §5A: Non-Blocking Image Processing.
/// </summary>
public sealed record ImageProcessingMessage(
    Guid PropertyId,
    Guid TenantId,
    string StorageKey,
    string OriginalFileName,
    string ContentType
);

/// <summary>
/// Abstraction over System.Threading.Channels.Channel&lt;ImageProcessingMessage&gt;.
/// Written by application handlers, read by the background service.
/// </summary>
public interface IImageProcessingChannel
{
    ValueTask WriteAsync(ImageProcessingMessage message, CancellationToken cancellationToken = default);
    IAsyncEnumerable<ImageProcessingMessage> ReadAllAsync(CancellationToken cancellationToken = default);
}
