using Ardalis.Result;
using MediatR;
using Microsoft.Extensions.Logging;
using Homeless.Application.DTOs;
using Homeless.Application.Interfaces;
using Homeless.Application.Mappers;
using Homeless.Domain.Interfaces.Repositories.Read;
using Homeless.Domain.Interfaces.Repositories.Write;

namespace Homeless.Application.UseCases.Properties;

public sealed class DeletePropertyImageHandler(
    IPropertyReadRepository propertyReadRepository,
    IPropertyWriteRepository propertyWriteRepository,
    IStorageService storageService,
    ITenantContext tenantContext,
    IUnitOfWork unitOfWork,
    ILogger<DeletePropertyImageHandler> logger)
    : IRequestHandler<DeletePropertyImageCommand, Result<PropertyResponse>>
{
    public async Task<Result<PropertyResponse>> Handle(
        DeletePropertyImageCommand request,
        CancellationToken cancellationToken)
    {
        var property = await propertyReadRepository
            .GetByIdAsync(request.PropertyId, cancellationToken)
            .ConfigureAwait(false);

        if (property is null)
            return Result.NotFound($"Property '{request.PropertyId}' not found.");

        if (property.TenantId != tenantContext.TenantId)
            return Result.Forbidden();

        var image = property.Images.FirstOrDefault(i => i.Key == request.ImageKey);
        if (image is null)
            return Result.NotFound($"Image not found on property.");

        property.RemoveImage(request.ImageKey);

        // Best-effort storage deletion — don't fail the request over a storage error
        try
        {
            await storageService.DeleteAsync(request.ImageKey, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Property image {ImageKey} could not be deleted from storage for property {PropertyId}. Reason: {Reason}. Continuing with metadata removal.",
                request.ImageKey,
                request.PropertyId,
                ex.Message);
        }

        await propertyWriteRepository.UpdateAsync(property, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return property.ToResponse().WithRawImageUrls(storageService);
    }
}
