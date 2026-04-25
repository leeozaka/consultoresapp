using Ardalis.Result;
using MediatR;
using Homeless.Application.DTOs;
using Homeless.Application.Interfaces;
using Homeless.Application.Mappers;
using Homeless.Domain.Interfaces.Repositories.Read;
using Homeless.Domain.Interfaces.Repositories.Write;

namespace Homeless.Application.UseCases.Properties;

public sealed class DeactivatePropertyHandler(
    IPropertyReadRepository propertyReadRepository,
    IPropertyWriteRepository propertyWriteRepository,
    ITenantContext tenantContext,
    IUnitOfWork unitOfWork,
    ICacheService cacheService)
    : IRequestHandler<DeactivatePropertyCommand, Result<PropertyResponse>>
{
    public async Task<Result<PropertyResponse>> Handle(
        DeactivatePropertyCommand request,
        CancellationToken cancellationToken)
    {
        var property = await propertyReadRepository
            .GetByIdAsync(request.PropertyId, cancellationToken)
            .ConfigureAwait(false);

        if (property is null)
            return Result.NotFound($"Property '{request.PropertyId}' not found.");

        if (property.TenantId != tenantContext.TenantId)
            return Result.Forbidden();

        property.Deactivate();

        await propertyWriteRepository.UpdateAsync(property, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await cacheService.RemoveAsync(CacheKeys.Property(request.PropertyId), cancellationToken).ConfigureAwait(false);

        return Result.Success(property.ToResponse());
    }
}
