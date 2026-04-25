using Microsoft.EntityFrameworkCore;
using Homeless.Domain.Entities;
using Homeless.Domain.Enums;
using Homeless.Domain.Interfaces.Repositories.Read;
using Homeless.Infrastructure.Persistence.Context;

namespace Homeless.Infrastructure.Persistence.Repositories.Read;

public sealed class PropertyReadRepository(ApplicationDbContext context) : IPropertyReadRepository
{
    public async Task<Property?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await context.Properties
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            .ConfigureAwait(false);

    public async Task<int> CountByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
        => await context.Properties
            .CountAsync(p => p.TenantId == tenantId, cancellationToken)
            .ConfigureAwait(false);

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        => await context.Properties
            .AnyAsync(p => p.Id == id, cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<Property>> GetStaleActivePropertiesAsync(
        DateTime publishedBefore, CancellationToken cancellationToken = default)
        => await context.Properties
            .IgnoreQueryFilters()
            .Where(p => p.Status == PropertyStatus.Active
                     && p.PublishedAtUtc != null
                     && p.PublishedAtUtc < publishedBefore)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}
