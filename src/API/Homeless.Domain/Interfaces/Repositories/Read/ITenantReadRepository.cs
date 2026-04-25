using Homeless.Domain.Entities;

namespace Homeless.Domain.Interfaces.Repositories.Read;

/// <summary>
/// Read-only tenant queries used by command handlers and workers that need the full entity.
/// Query handlers that only need DTO data should use ITenantQueryService instead.
/// Implementations must not track returned entities.
/// </summary>
public interface ITenantReadRepository
{
    Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Tenant?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<Tenant?> GetByCustomDomainAsync(string domain, CancellationToken cancellationToken = default);
    Task<bool> ExistsBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<bool> ExistsByCustomDomainAsync(string domain, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns active tenants whose NextBillingDate is more than <paramref name="graceDays"/> days in the past.
    /// </summary>
    Task<IReadOnlyList<Tenant>> GetOverdueTenantsAsync(int graceDays, CancellationToken cancellationToken = default);
}
