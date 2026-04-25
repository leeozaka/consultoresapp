namespace Homeless.Domain.Entities;

/// <summary>
/// Marker interface for all tenant-scoped domain entities.
/// EF Core Global Query Filters and PostgreSQL RLS both rely on this contract.
/// </summary>
public interface ITenantEntity
{
    Guid TenantId { get; set; }
}
