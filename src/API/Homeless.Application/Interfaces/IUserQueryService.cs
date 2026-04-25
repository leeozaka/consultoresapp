using Homeless.Application.DTOs;

namespace Homeless.Application.Interfaces;

/// <summary>
/// Read-only projection of platform users with role/tenant context.
/// </summary>
public interface IUserQueryService
{
    Task<IReadOnlyList<UserResponse>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserResponse>> GetOrphanedAsync(CancellationToken cancellationToken = default);

    Task<UserResponse?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserResponse>> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
