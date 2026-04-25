using System.Text.Json.Serialization;
using Homeless.Domain.Enums;

namespace Homeless.Application.DTOs;

public sealed record PropertySearchRequest(
    [property: JsonPropertyName("city")] string? City = null,
    [property: JsonPropertyName("neighbourhood")] string? Neighbourhood = null,
    [property: JsonPropertyName("min_price")] decimal? MinPrice = null,
    [property: JsonPropertyName("max_price")] decimal? MaxPrice = null,
    [property: JsonPropertyName("min_bedrooms")] int? MinBedrooms = null,
    [property: JsonPropertyName("property_type")] PropertyType? PropertyType = null,
    [property: JsonPropertyName("listing_type")] ListingType? ListingType = null,
    [property: JsonPropertyName("status")] PropertyStatus? Status = null,
    [property: JsonPropertyName("page")] int Page = 1,
    [property: JsonPropertyName("page_size")] int PageSize = 20
);
