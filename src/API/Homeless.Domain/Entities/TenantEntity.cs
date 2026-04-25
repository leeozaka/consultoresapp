namespace Homeless.Domain.Entities;

/// <summary>
/// Base class for all tenant-scoped entities.
/// Inherits from <see cref="Entity"/> (which carries Id, CreatedAt, UpdatedAt and domain events).
/// The financial domain entities (Account, Transaction, Client) remain on Entity directly —
/// they are system-scoped billing records. All real estate entities inherit from TenantEntity.
/// </summary>
public abstract class TenantEntity : Entity, ITenantEntity
{
    /// <summary>
    /// The owning tenant. Automatically stamped by ApplicationDbContext.SaveChangesAsync
    /// from ITenantContext — never trust client payloads to set this value.
    /// </summary>
    public Guid TenantId { get; set; }
}
