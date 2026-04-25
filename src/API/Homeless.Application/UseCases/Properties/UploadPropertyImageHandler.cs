using Ardalis.Result;
using MediatR;
using Microsoft.Extensions.Logging;
using Homeless.Application.DTOs;
using Homeless.Application.Interfaces;
using Homeless.Application.Mappers;
using Homeless.Domain.Interfaces.Repositories.Read;
using Homeless.Domain.Interfaces.Repositories.Write;

namespace Homeless.Application.UseCases.Properties;

public sealed class UploadPropertyImageHandler(
    IPropertyReadRepository propertyReadRepository,
    IPropertyWriteRepository propertyWriteRepository,
    IStorageService storageService,
    IImageProcessingChannel imageChannel,
    ITenantContext tenantContext,
    IUnitOfWork unitOfWork,
    ILogger<UploadPropertyImageHandler> logger)
    : IRequestHandler<UploadPropertyImageCommand, Result<PropertyResponse>>
{
    private static readonly long MaxFileSizeBytes = 20 * 1024 * 1024; // 20 MB
    private static readonly HashSet<string> AllowedContentTypes = ["image/jpeg", "image/png", "image/webp", "image/gif"];

    public async Task<Result<PropertyResponse>> Handle(
        UploadPropertyImageCommand request,
        CancellationToken cancellationToken)
    {
        if (request.FileSizeBytes > MaxFileSizeBytes)
            return Result.Invalid(new ValidationError($"Image exceeds maximum size of {MaxFileSizeBytes / 1024 / 1024} MB."));

        if (!AllowedContentTypes.Contains(request.ContentType.ToLowerInvariant()))
            return Result.Invalid(new ValidationError($"Unsupported image type '{request.ContentType}'."));

        var property = await propertyReadRepository
            .GetByIdAsync(request.PropertyId, cancellationToken)
            .ConfigureAwait(false);

        if (property is null)
            return Result.NotFound($"Property '{request.PropertyId}' not found.");

        if (property.TenantId != tenantContext.TenantId)
            return Result.Forbidden();

        // Upload raw to storage (R2 in prod, local in dev)
        var extension = Path.GetExtension(request.OriginalFileName);
        var storageKey = $"tenants/{tenantContext.TenantId}/properties/{property.Id}/raw/{Guid.NewGuid()}{extension}";

        await storageService
            .UploadAsync(request.ImageStream, storageKey, request.ContentType, cancellationToken)
            .ConfigureAwait(false);

        // Add pending image to property domain model
        property.AddImage(storageKey, request.OriginalFileName);

        await propertyWriteRepository.UpdateAsync(property, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Publish to processing channel (non-blocking, returns 202)
        var message = new ImageProcessingMessage(
            PropertyId: property.Id,
            TenantId: tenantContext.TenantId,
            StorageKey: storageKey,
            OriginalFileName: request.OriginalFileName,
            ContentType: request.ContentType);

        await imageChannel
            .WriteAsync(message, cancellationToken)
            .ConfigureAwait(false);

        logger.LogInformation(
            "Image {StorageKey} queued for processing (property {PropertyId})",
            storageKey, property.Id);

        // Mirror DeletePropertyImageHandler: return raw CDN URL as a temporary
        // fallback so the browser can display the image while the background
        // processor generates the compressed WebP variants.
        return Result.Success(property.ToResponse().WithRawImageUrls(storageService));
    }
}
