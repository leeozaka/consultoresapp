using Homeless.Domain.Entities;

namespace Homeless.Domain.Interfaces.Repositories.Read;

/// <summary>
/// Read-only property queries used by command handlers and workers that need the full entity.
/// Query handlers that only need DTO data should use IPropertyQueryService instead.
/// Implementations must not track returned entities.
/// </summary>
public interface IPropertyReadRepository
{
    Task<Property?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<int> CountByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns active properties whose PublishedAtUtc is before <paramref name="publishedBefore"/>.
    /// Used by the maintenance worker to deactivate stale listings.
    /// </summary>
    Task<IReadOnlyList<Property>> GetStaleActivePropertiesAsync(
        DateTime publishedBefore, CancellationToken cancellationToken = default);
}
