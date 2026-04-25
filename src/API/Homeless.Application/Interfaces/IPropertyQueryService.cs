using Homeless.Application.DTOs;
using Homeless.Domain.ValueObjects;

namespace Homeless.Application.Interfaces;

/// <summary>
/// Application-layer query service for property reads.
/// Returns projected DTOs directly so EF Core SELECTs only the columns
/// present in <see cref="PropertyResponse"/>.
/// </summary>
public interface IPropertyQueryService
{
    Task<PropertyResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<PropertyResponse> Items, int TotalCount)> SearchAsync(
        Guid tenantId,
        PropertyFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<FeaturedPropertyResponse> Items, int TotalCount)> GetFeaturedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LocationSuggestionResponse>> GetLocationSuggestionsAsync(
        Guid tenantId,
        string? query,
        CancellationToken cancellationToken = default);
}
