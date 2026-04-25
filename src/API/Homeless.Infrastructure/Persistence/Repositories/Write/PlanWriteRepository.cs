using Homeless.Domain.Entities;
using Homeless.Domain.Interfaces.Repositories.Write;
using Homeless.Infrastructure.Persistence.Context;

namespace Homeless.Infrastructure.Persistence.Repositories.Write;

public sealed class PlanWriteRepository(ApplicationDbContext context) : IPlanWriteRepository
{
    public async Task AddAsync(Plan plan, CancellationToken cancellationToken = default)
    {
        await context.Plans.AddAsync(plan, cancellationToken).ConfigureAwait(false);
    }

    public Task UpdateAsync(Plan plan, CancellationToken cancellationToken = default)
    {
        context.Plans.Update(plan);
        return Task.CompletedTask;
    }
}
