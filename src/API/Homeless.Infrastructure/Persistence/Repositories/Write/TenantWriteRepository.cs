using Homeless.Domain.Entities;
using Homeless.Domain.Interfaces.Repositories.Write;
using Homeless.Infrastructure.Persistence.Context;

namespace Homeless.Infrastructure.Persistence.Repositories.Write;

public sealed class TenantWriteRepository(ApplicationDbContext context) : ITenantWriteRepository
{
    public async ValueTask AddAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        await context.Tenants
            .AddAsync(tenant, cancellationToken)
            .ConfigureAwait(false);
    }

    public ValueTask UpdateAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        context.Tenants.Update(tenant);
        return ValueTask.CompletedTask;
    }

    public async ValueTask DeleteAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var tenant = await context.Tenants
            .FindAsync([tenantId], cancellationToken)
            .ConfigureAwait(false);

        if (tenant is not null)
            context.Tenants.Remove(tenant);
    }
}
