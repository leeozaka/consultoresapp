using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
using Homeless.Application.DTOs;
using Homeless.Application.Interfaces;
using Homeless.Infrastructure.Messaging;
using Homeless.Infrastructure.Persistence.Context;

namespace Homeless.Infrastructure.Messaging;

/// <summary>
/// Long-running BackgroundService that reads from the IImageProcessingChannel,
/// generates WebP thumbnails/medium/full via ImageSharp, stores variants back to R2,
/// then updates the Property aggregate with processed URLs.
/// Each message is processed in an isolated DI scope.
/// </summary>
public sealed class ImageProcessingBackgroundService(
    IImageProcessingChannel channel,
    IImageProcessedStream processedStream,
    IServiceScopeFactory scopeFactory,
    IOptions<ImageProcessingOptions> options,
    ILogger<ImageProcessingBackgroundService> logger)
    : BackgroundService
{
    private readonly ImageProcessingOptions _opts = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Image processing background service started");

        await foreach (var message in channel.ReadAllAsync(stoppingToken))
        {
            try
            {
                await ProcessMessageAsync(message, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Failed to process image {Key} for property {PropertyId}",
                    message.StorageKey, message.PropertyId);
            }
        }

        logger.LogInformation("Image processing background service stopped");
    }

    private async Task ProcessMessageAsync(
        ImageProcessingMessage message,
        CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var storage = scope.ServiceProvider.GetRequiredService<IStorageService>();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();

        // Fetch raw image from storage
        await using var rawStream = await storage
            .GetAsync(message.StorageKey, cancellationToken)
            .ConfigureAwait(false);

        using var image = await Image.LoadAsync(rawStream, cancellationToken).ConfigureAwait(false);

        var baseKey = Path.GetFileNameWithoutExtension(message.StorageKey);
        var keyPrefix = Path.GetDirectoryName(message.StorageKey)?.Replace('\\', '/') ?? string.Empty;

        var thumbnailKey = $"{keyPrefix}/{baseKey}_thumb.webp";
        var mediumKey = $"{keyPrefix}/{baseKey}_medium.webp";
        var fullKey = $"{keyPrefix}/{baseKey}_full.webp";

        var thumbnailUrl = await EncodeAndUploadAsync(
            image, storage, thumbnailKey, _opts.ThumbnailWidth, cancellationToken).ConfigureAwait(false);

        var mediumUrl = await EncodeAndUploadAsync(
            image, storage, mediumKey, _opts.MediumWidth, cancellationToken).ConfigureAwait(false);

        var fullUrl = await EncodeAndUploadAsync(
            image, storage, fullKey, _opts.FullWidth, cancellationToken).ConfigureAwait(false);

        // Update the Property aggregate
        var property = await db.Properties
            .FirstOrDefaultAsync(p => p.Id == message.PropertyId, cancellationToken)
            .ConfigureAwait(false);

        if (property is null)
        {
            logger.LogWarning(
                "Property {PropertyId} not found when marking image {Key} processed",
                message.PropertyId, message.StorageKey);
            return;
        }

        property.MarkImageProcessed(
            message.StorageKey,
            url: fullUrl,
            thumbnailUrl: thumbnailUrl,
            mediumUrl: mediumUrl);

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Invalidate the property cache so the next GET returns is_processed=true
        await cache.RemoveAsync(
            CacheKeys.Property(message.PropertyId), cancellationToken).ConfigureAwait(false);

        // Push real-time SSE notification to connected clients
        await processedStream.PublishAsync(
            new ImageProcessedEvent(
                PropertyId: message.PropertyId,
                ImageKey: message.StorageKey,
                Url: fullUrl,
                ThumbnailUrl: thumbnailUrl,
                MediumUrl: mediumUrl,
                IsProcessed: true),
            cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Processed image {Key} for property {PropertyId}",
            message.StorageKey, message.PropertyId);
    }

    private async Task<string> EncodeAndUploadAsync(
        Image source,
        IStorageService storage,
        string key,
        int maxWidth,
        CancellationToken cancellationToken)
    {
        using var clone = source.Clone(ctx =>
        {
            if (source.Width > maxWidth)
                ctx.Resize(new ResizeOptions { Size = new SixLabors.ImageSharp.Size(maxWidth, 0), Mode = ResizeMode.Max });
        });

        using var ms = new MemoryStream();
        await clone.SaveAsWebpAsync(ms, new WebpEncoder { Quality = _opts.WebPQuality }, cancellationToken)
            .ConfigureAwait(false);
        ms.Position = 0;

        return await storage.UploadAsync(ms, key, "image/webp", cancellationToken).ConfigureAwait(false);
    }
}
