# Handler Examples

Full, annotated examples of command handlers and query handlers as used in this project.

---

## Command Handler

```csharp
using Ardalis.Result;
using MediatR;
using Homeless.Application.DTOs;
using Homeless.Application.Interfaces;
using Homeless.Application.Mappers;
using Homeless.Domain.Entities;
using Homeless.Domain.Interfaces.Repositories.Read;
using Homeless.Domain.Interfaces.Repositories.Write;

namespace Homeless.Application.UseCases.Properties;

public sealed class CreatePropertyHandler(
    IPropertyReadRepository  propertyReadRepository,   // needed for entitlement check
    IPropertyWriteRepository propertyWriteRepository,  // write-side
    IEntitlementService      entitlementService,
    ITenantContext           tenantContext,
    ICurrentUserService      currentUser,
    IUnitOfWork              unitOfWork)
    : IRequestHandler<CreatePropertyCommand, Result<PropertyResponse>>
{
    public async Task<Result<PropertyResponse>> Handle(
        CreatePropertyCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Enforce plan entitlement (tenant-scoped read)
        var maxProperties = await entitlementService
            .GetEntitlementAsync<int?>(tenantContext.TenantId, "max_properties", cancellationToken)
            .ConfigureAwait(false);

        if (maxProperties.HasValue && maxProperties > 0)
        {
            var count = await propertyReadRepository
                .CountByTenantAsync(tenantContext.TenantId, cancellationToken)
                .ConfigureAwait(false);

            if (count >= maxProperties)
                return Result.Forbidden($"Your plan allows a maximum of {maxProperties} properties.");
        }

        // 2. Create the domain entity (factory method on the aggregate root)
        var property = Property.Create(
            tenantId:    tenantContext.TenantId,   // ← always from context, never from request
            title:       request.Data.Title,
            price:       request.Data.Price,
            city:        request.Data.City,
            state:       request.Data.State,
            propertyType: request.Data.PropertyType,
            listingType: request.Data.ListingType,
            bedrooms:    request.Data.Bedrooms,
            bathrooms:   request.Data.Bathrooms,
            agentId:     currentUser.IsAuthenticated ? currentUser.UserId : null);

        // 3. Persist via write repository, then save
        await propertyWriteRepository.AddAsync(property, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // 4. Map domain → response using Mapperly; return Result.Created for 201
        return Result.Created(property.ToResponse());
    }
}
```

### Key points for command handlers

| Concern | Rule |
|---------|------|
| Tenant ID | Always `tenantContext.TenantId`, never from `request` |
| Write + read repos | OK to inject both when you need a pre-mutation check |
| SaveChanges | Called once, after all writes, before returning |
| Result status | `Result.Created` for new resources, `Result.Success` for updates/deletes |
| Mapping | Delegate to the `{Domain}Mapper` extension method; no inline `new Dto { }` |

---

## Query Handler

```csharp
using Ardalis.Result;
using MediatR;
using Homeless.Application.DTOs;
using Homeless.Application.Interfaces;
using Homeless.Application.Mappers;
using Homeless.Domain.Interfaces.Repositories.Read;

namespace Homeless.Application.UseCases.Properties;

public sealed class SearchPropertiesHandler(
    IPropertyReadRepository propertyRepository,
    ITenantContext          tenantContext,
    IStorageService         storage)           // only inject what you actually use
    : IRequestHandler<SearchPropertiesQuery, Result<PaginatedResponse<PropertyResponse>>>
{
    public async Task<Result<PaginatedResponse<PropertyResponse>>> Handle(
        SearchPropertiesQuery request,
        CancellationToken cancellationToken)
    {
        // 1. Delegate to the read repo, always pass TenantId
        var (items, totalCount) = await propertyRepository.SearchAsync(
            tenantId:          tenantContext.TenantId,
            filter:            request.Filter,
            page:              request.Page,
            pageSize:          request.PageSize,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        // 2. Map domain entities → response DTOs
        var responses = items
            .Select(p => p.ToResponse().WithRawImageUrls(storage))
            .ToList()
            .AsReadOnly();

        // 3. Wrap in pagination envelope
        var paginated = PaginatedResponse<PropertyResponse>.Create(
            responses, totalCount, request.Page, request.PageSize);

        return Result.Success(paginated);
    }
}
```

### Key points for query handlers

| Concern | Rule |
|---------|------|
| Repositories | Read-only (`IXxxReadRepository`). **Never** inject a write repo or `IUnitOfWork`. |
| Tenant ID | Same rule: always from `ITenantContext`, not from `request` |
| Pagination | `PaginatedResponse<T>.Create(items, total, page, pageSize)` |
| Point-lookup not found | Return `Result.NotFound()` when the entity doesn't exist |

---

## Point-Lookup Query (non-paginated)

```csharp
public sealed class GetPropertyHandler(
    IPropertyReadRepository repo,
    ITenantContext           tenantContext)
    : IRequestHandler<GetPropertyQuery, Result<PropertyResponse>>
{
    public async Task<Result<PropertyResponse>> Handle(
        GetPropertyQuery request,
        CancellationToken cancellationToken)
    {
        var property = await repo
            .GetByIdAsync(request.Id, tenantContext.TenantId, cancellationToken)
            .ConfigureAwait(false);

        return property is null
            ? Result.NotFound()
            : Result.Success(property.ToResponse());
    }
}
```

---

## Update Command (find-then-mutate pattern)

```csharp
public sealed class UpdatePropertyHandler(
    IPropertyReadRepository  propertyReadRepository,
    IPropertyWriteRepository propertyWriteRepository,
    ITenantContext           tenantContext,
    ICurrentUserService      currentUser,
    IUnitOfWork              unitOfWork)
    : IRequestHandler<UpdatePropertyCommand, Result<PropertyResponse>>
{
    public async Task<Result<PropertyResponse>> Handle(
        UpdatePropertyCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Load — scope by tenant to prevent cross-tenant access
        var property = await propertyReadRepository
            .GetByIdAsync(request.Id, tenantContext.TenantId, cancellationToken)
            .ConfigureAwait(false);

        if (property is null)
            return Result.NotFound();

        // 2. Authorise (optional: only the owning agent may edit)
        if (property.AgentId is not null &&
            property.AgentId != currentUser.UserId &&
            !currentUser.IsInRole(Roles.TenantAdmin))
            return Result.Forbidden("Only the assigned agent or a tenant admin can update this property.");

        // 3. Mutate via domain method
        property.UpdateDetails(
            title:        request.Data.Title,
            description:  request.Data.Description,
            price:        request.Data.Price,
            city:         request.Data.City,
            state:        request.Data.State,
            propertyType: request.Data.PropertyType,
            listingType:  request.Data.ListingType,
            zipCode:      request.Data.ZipCode,
            address:      request.Data.Address,
            bedrooms:     request.Data.Bedrooms,
            bathrooms:    request.Data.Bathrooms,
            parkingSpaces: request.Data.ParkingSpaces,
            areaSqMeters: request.Data.AreaSqMeters,
            attributes:   request.Data.Attributes);

        // 4. The write repository tracks the change; save and return
        await propertyWriteRepository.UpdateAsync(property, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(property.ToResponse());
    }
}
```

---

## Delete Command

```csharp
public sealed class DeletePropertyHandler(
    IPropertyReadRepository  propertyReadRepository,
    IPropertyWriteRepository propertyWriteRepository,
    ITenantContext           tenantContext,
    IUnitOfWork              unitOfWork)
    : IRequestHandler<DeletePropertyCommand, Result>
{
    public async Task<Result> Handle(
        DeletePropertyCommand request,
        CancellationToken cancellationToken)
    {
        var property = await propertyReadRepository
            .GetByIdAsync(request.Id, tenantContext.TenantId, cancellationToken)
            .ConfigureAwait(false);

        if (property is null)
            return Result.NotFound();

        await propertyWriteRepository.DeleteAsync(property, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
```

Note: `Result` (non-generic) is used for operations with no response body; the controller returns `204 No Content`.
