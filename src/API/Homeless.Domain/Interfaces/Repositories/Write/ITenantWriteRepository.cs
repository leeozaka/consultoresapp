using Homeless.Domain.Entities;

namespace Homeless.Domain.Interfaces.Repositories.Write;

/// <summary>
/// Write-side tenant persistence. Implementations use change-tracking or explicit attach.
/// </summary>
public interface ITenantWriteRepository
{
    ValueTask AddAsync(Tenant tenant, CancellationToken cancellationToken = default);
    ValueTask UpdateAsync(Tenant tenant, CancellationToken cancellationToken = default);
    ValueTask DeleteAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
