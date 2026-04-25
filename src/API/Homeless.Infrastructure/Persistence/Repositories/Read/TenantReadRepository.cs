using Microsoft.EntityFrameworkCore;
using Homeless.Domain.Entities;
using Homeless.Domain.Enums;
using Homeless.Domain.Interfaces.Repositories.Read;
using Homeless.Infrastructure.Persistence.Context;

namespace Homeless.Infrastructure.Persistence.Repositories.Read;

public sealed class TenantReadRepository(ApplicationDbContext context) : ITenantReadRepository
{
    public async Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await context.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
            .ConfigureAwait(false);

    public async Task<Tenant?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
        => await context.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Slug == slug, cancellationToken)
            .ConfigureAwait(false);

    public async Task<Tenant?> GetByCustomDomainAsync(string domain, CancellationToken cancellationToken = default)
        => await context.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.CustomDomain == domain, cancellationToken)
            .ConfigureAwait(false);

    public async Task<bool> ExistsBySlugAsync(string slug, CancellationToken cancellationToken = default)
        => await context.Tenants
            .AnyAsync(t => t.Slug == slug, cancellationToken)
            .ConfigureAwait(false);

    public async Task<bool> ExistsByCustomDomainAsync(string domain, CancellationToken cancellationToken = default)
        => await context.Tenants
            .AnyAsync(t => t.CustomDomain == domain, cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<Tenant>> GetOverdueTenantsAsync(
        int graceDays, CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-graceDays);
        return await context.Tenants
            .Where(t => t.Status == TenantStatus.Active
                     && t.NextBillingDate != null
                     && t.NextBillingDate < cutoff)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
