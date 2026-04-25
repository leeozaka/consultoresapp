using Homeless.Application.DTOs;
using Homeless.Domain.Enums;

namespace Homeless.Application.Interfaces;

/// <summary>
/// Application-layer query service for tenant reads.
/// Returns projected DTOs directly so EF Core SELECTs only the columns
/// present in <see cref="TenantResponse"/>.
/// </summary>
public interface ITenantQueryService
{
    Task<TenantResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<TenantResponse?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

    Task<TenantResponse?> GetByCustomDomainAsync(string domain, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TenantResponse>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TenantResponse>> GetByStatusAsync(TenantStatus status, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<TenantResponse> Items, bool HasNextPage)> GetPagedAsync(
        int pageSize,
        string? afterName,
        Guid? afterId,
        CancellationToken cancellationToken = default);
}
