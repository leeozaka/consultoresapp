using Homeless.Application.DTOs;

namespace Homeless.Application.Interfaces;

/// <summary>
/// Pub/sub stream for image-processed SSE notifications.
/// Published by ImageProcessingBackgroundService after variant generation;
/// read by the SSE endpoint to push real-time updates to the SPA.
/// </summary>
public interface IImageProcessedStream
{
    ValueTask PublishAsync(ImageProcessedEvent processedEvent, CancellationToken cancellationToken = default);
    IAsyncEnumerable<ImageProcessedEvent> ReadAllAsync(Guid propertyId, CancellationToken cancellationToken = default);
}
