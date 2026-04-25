using Ardalis.Result;
using MediatR;
using Homeless.Application.DTOs;
using Homeless.Application.Interfaces;
using Homeless.Domain.Interfaces.Repositories.Read;

namespace Homeless.Application.UseCases.Tenants;

public sealed class UploadTenantPortalImageHandler(
    IStorageService storageService,
    ITenantContext tenantContext,
    ITenantReadRepository tenantReadRepository)
    : IRequestHandler<UploadTenantPortalImageCommand, Result<PortalAssetUploadResponse>>
{
    private static readonly long MaxFileSizeBytes = 10 * 1024 * 1024;
    private static readonly HashSet<string> AllowedContentTypes = ["image/jpeg", "image/png", "image/webp", "image/gif"];

    public async Task<Result<PortalAssetUploadResponse>> Handle(
        UploadTenantPortalImageCommand request,
        CancellationToken cancellationToken)
    {
        if (!tenantContext.IsResolved)
            return Result.NotFound("No tenant resolved for this request.");

        var tenant = await tenantReadRepository
            .GetByIdAsync(tenantContext.TenantId, cancellationToken)
            .ConfigureAwait(false);

        if (tenant is null)
            return Result.NotFound($"Tenant '{tenantContext.TenantId}' not found.");

        if (tenant.FrontendOrigin is { Length: > 0 })
            return Result.Forbidden();

        if (request.FileSizeBytes > MaxFileSizeBytes)
            return Result.Invalid(new ValidationError($"Image exceeds maximum size of {MaxFileSizeBytes / 1024 / 1024} MB."));

        if (!AllowedContentTypes.Contains(request.ContentType.ToLowerInvariant()))
            return Result.Invalid(new ValidationError($"Unsupported image type '{request.ContentType}'."));

        var extension = Path.GetExtension(request.OriginalFileName);
        var storageKey = $"tenants/{tenantContext.TenantId}/portal/{Guid.NewGuid()}{extension}";

        await storageService
            .UploadAsync(request.ImageStream, storageKey, request.ContentType, cancellationToken)
            .ConfigureAwait(false);

        var url = storageService.GetPublicUrl(storageKey);
        return Result.Success(new PortalAssetUploadResponse(url));
    }
}
