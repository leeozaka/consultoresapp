using Ardalis.Result;
using MediatR;
using Homeless.Application.DTOs;
using Homeless.Application.Interfaces;
using Homeless.Application.Mappers;
using Homeless.Domain.Interfaces.Repositories.Read;
using Homeless.Domain.Interfaces.Repositories.Write;
using Homeless.Domain.ValueObjects;

namespace Homeless.Application.UseCases.Properties;

public sealed class UpdatePropertyHandler(
    IPropertyReadRepository propertyReadRepository,
    IPropertyWriteRepository propertyWriteRepository,
    IEntitlementService entitlementService,
    ITenantContext tenantContext,
    IUnitOfWork unitOfWork,
    ICacheService cacheService)
    : IRequestHandler<UpdatePropertyCommand, Result<PropertyResponse>>
{
    public async Task<Result<PropertyResponse>> Handle(
        UpdatePropertyCommand request,
        CancellationToken cancellationToken)
    {
        var property = await propertyReadRepository
            .GetByIdAsync(request.PropertyId, cancellationToken)
            .ConfigureAwait(false);

        if (property is null)
            return Result.NotFound($"Property '{request.PropertyId}' not found.");

        if (property.TenantId != tenantContext.TenantId)
            return Result.Forbidden();

        var price = Money.Create((long)(request.Data.Price * 100), property.Price.Currency);
        property.UpdateDetails(
            title: request.Data.Title,
            description: request.Data.Description,
            price: price,
            city: request.Data.City,
            state: request.Data.State,
            propertyType: request.Data.PropertyType,
            listingType: request.Data.ListingType,
            zipCode: request.Data.ZipCode,
            address: request.Data.Address,
            bedrooms: request.Data.Bedrooms,
            bathrooms: request.Data.Bathrooms,
            parkingSpaces: request.Data.ParkingSpaces,
            areaSqMeters: request.Data.AreaSqMeters,
            attributes: request.Data.Attributes,
            contactPhone: request.Data.ContactPhone,
            neighbourhood: request.Data.Neighbourhood);

        await propertyWriteRepository.UpdateAsync(property, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await cacheService.RemoveAsync(CacheKeys.Property(request.PropertyId), cancellationToken).ConfigureAwait(false);

        return Result.Success(property.ToResponse());
    }
}
