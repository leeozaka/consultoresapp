using Homeless.Domain.Entities;

namespace Homeless.Domain.Interfaces.Repositories.Read;

public interface IPlanReadRepository
{
    Task<IReadOnlyList<Plan>> GetActivePlansAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Plan>> GetAllPlansAsync(CancellationToken cancellationToken = default);
    Task<Plan?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
