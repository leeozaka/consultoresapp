using Homeless.Application.DTOs;

namespace Homeless.Application.Interfaces;

/// <summary>
/// Application-layer query service for plan reads.
/// Returns projected DTOs directly so EF Core SELECTs only the columns
/// present in <see cref="PlanResponse"/>.
/// </summary>
public interface IPlanQueryService
{
    Task<IReadOnlyList<PlanResponse>> GetActivePlansAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PlanResponse>> GetAllPlansAsync(CancellationToken cancellationToken = default);

    Task<PlanResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
