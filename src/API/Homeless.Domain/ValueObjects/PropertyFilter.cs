using Homeless.Domain.Enums;

namespace Homeless.Domain.ValueObjects;

/// <summary>
/// Encapsulates the search/filter criteria for property listings.
/// Replaces the long parameter list on <c>IPropertyRepository.SearchAsync</c>.
/// </summary>
public sealed record PropertyFilter(
    string? City = null,
    string? Neighbourhood = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    int? MinBedrooms = null,
    PropertyType? PropertyType = null,
    ListingType? ListingType = null,
    PropertyStatus? Status = null
);
