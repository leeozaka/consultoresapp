using Homeless.Application.DTOs;
using Homeless.Domain.Entities;
using Homeless.Domain.ValueObjects;
using Riok.Mapperly.Abstractions;

namespace Homeless.Application.Mappers;

[Mapper]
public static partial class TenantMapper
{
    [MapperIgnoreSource(nameof(Tenant.DomainEvents))]
    [MapperIgnoreSource(nameof(Tenant.HasPendingPayment))]
    [MapperIgnoreSource(nameof(Tenant.HasRecurringPayment))]
    [MapperIgnoreSource(nameof(Tenant.IsDeleted))]
    [MapperIgnoreSource(nameof(Tenant.DeletedAt))]
    public static partial TenantResponse ToResponse(this Tenant tenant);

    public static partial BrandingConfigResponse ToResponse(this BrandingConfig config);
}
