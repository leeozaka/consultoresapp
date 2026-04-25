using Ardalis.Result;
using MediatR;
using Homeless.Application.DTOs;
using Homeless.Application.Interfaces;
using Homeless.Application.Mappers;
using Homeless.Domain.Entities;
using Homeless.Domain.Interfaces.Repositories.Read;
using Homeless.Domain.Interfaces.Repositories.Write;
using Homeless.Domain.ValueObjects;

namespace Homeless.Application.UseCases.Properties;

public sealed class CreatePropertyHandler(
    IPropertyReadRepository propertyReadRepository,
    IPropertyWriteRepository propertyWriteRepository,
    IEntitlementService entitlementService,
    ITenantContext tenantContext,
    ICurrentUserService currentUser,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreatePropertyCommand, Result<PropertyResponse>>
{
    public async Task<Result<PropertyResponse>> Handle(
        CreatePropertyCommand request,
        CancellationToken cancellationToken)
    {
        // Enforce max_properties entitlement
        var maxProperties = await entitlementService
            .GetEntitlementAsync<int?>(tenantContext.TenantId, "max_properties", cancellationToken)
            .ConfigureAwait(false);

        if (maxProperties.HasValue && maxProperties > 0)
        {
            var currentCount = await propertyReadRepository
                .CountByTenantAsync(tenantContext.TenantId, cancellationToken)
                .ConfigureAwait(false);

            if (currentCount >= maxProperties)
                return Result.Forbidden($"Your plan allows a maximum of {maxProperties} properties.");
        }

        var price = Money.Create((long)(request.Data.Price * 100), Currency.BRL);
        var property = Property.Create(
            tenantId: tenantContext.TenantId,
            title: request.Data.Title,
            price: price,
            city: request.Data.City,
            state: request.Data.State,
            propertyType: request.Data.PropertyType,
            listingType: request.Data.ListingType,
            bedrooms: request.Data.Bedrooms,
            bathrooms: request.Data.Bathrooms,
            agentId: currentUser.IsAuthenticated ? currentUser.UserId : null);

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

        await propertyWriteRepository.AddAsync(property, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Created(property.ToResponse());
    }
}
