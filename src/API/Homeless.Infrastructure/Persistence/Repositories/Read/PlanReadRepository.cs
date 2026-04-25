using Microsoft.EntityFrameworkCore;
using Homeless.Domain.Entities;
using Homeless.Domain.Interfaces.Repositories.Read;
using Homeless.Infrastructure.Persistence.Context;

namespace Homeless.Infrastructure.Persistence.Repositories.Read;

public sealed class PlanReadRepository(ApplicationDbContext context) : IPlanReadRepository
{
    public async Task<IReadOnlyList<Plan>> GetActivePlansAsync(CancellationToken cancellationToken = default) =>
        await context.Plans
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Price.AmountInCents)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<Plan>> GetAllPlansAsync(CancellationToken cancellationToken = default) =>
        await context.Plans
            .AsNoTracking()
            .OrderBy(p => p.Price.AmountInCents)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<Plan?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await context.Plans
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            .ConfigureAwait(false);
}
