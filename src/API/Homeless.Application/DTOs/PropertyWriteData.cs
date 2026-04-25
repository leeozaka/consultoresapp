using System.Text.Json.Serialization;
using Homeless.Domain.Enums;

namespace Homeless.Application.DTOs;

/// <summary>
/// Shared write payload for create and update property operations.
/// Eliminates the duplicated 14-parameter list across <c>CreatePropertyCommand</c>
/// and <c>UpdatePropertyCommand</c>.
/// </summary>
public sealed record PropertyWriteData(
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("price")] decimal Price,
    [property: JsonPropertyName("city")] string City,
    [property: JsonPropertyName("state")] string State,
    [property: JsonPropertyName("property_type")] PropertyType PropertyType,
    [property: JsonPropertyName("listing_type")] ListingType ListingType,
    [property: JsonPropertyName("bedrooms")] int Bedrooms,
    [property: JsonPropertyName("bathrooms")] int Bathrooms,
    [property: JsonPropertyName("description")] string? Description = null,
    [property: JsonPropertyName("zip_code")] string? ZipCode = null,
    [property: JsonPropertyName("address")] string? Address = null,
    [property: JsonPropertyName("parking_spaces")] int? ParkingSpaces = null,
    [property: JsonPropertyName("area_sq_meters")] decimal? AreaSqMeters = null,
    [property: JsonPropertyName("attributes")] Dictionary<string, object>? Attributes = null,
    [property: JsonPropertyName("contact_phone")] string? ContactPhone = null,
    [property: JsonPropertyName("neighbourhood")] string? Neighbourhood = null
);
