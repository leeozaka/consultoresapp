using Homeless.Domain.Entities;
using Homeless.Domain.Interfaces.Repositories.Write;
using Homeless.Infrastructure.Persistence.Context;

namespace Homeless.Infrastructure.Persistence.Repositories.Write;

public sealed class PropertyWriteRepository(ApplicationDbContext context) : IPropertyWriteRepository
{
    public async ValueTask AddAsync(Property property, CancellationToken cancellationToken = default)
    {
        await context.Properties
            .AddAsync(property, cancellationToken)
            .ConfigureAwait(false);
    }

    public ValueTask UpdateAsync(Property property, CancellationToken cancellationToken = default)
    {
        context.Properties.Update(property);
        return ValueTask.CompletedTask;
    }
}
