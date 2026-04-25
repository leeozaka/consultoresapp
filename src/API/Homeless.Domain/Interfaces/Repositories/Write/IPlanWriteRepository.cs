using Homeless.Domain.Entities;

namespace Homeless.Domain.Interfaces.Repositories.Write;

public interface IPlanWriteRepository
{
    Task AddAsync(Plan plan, CancellationToken cancellationToken = default);
    Task UpdateAsync(Plan plan, CancellationToken cancellationToken = default);
}
