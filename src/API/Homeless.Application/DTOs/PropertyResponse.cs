using System.Text.Json.Serialization;
using Homeless.Domain.Enums;
using Homeless.Domain.ValueObjects;

namespace Homeless.Application.DTOs;

public sealed record PropertyResponse(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("price")] decimal Price,
    [property: JsonPropertyName("currency")] string Currency,
    [property: JsonPropertyName("city")] string City,
    [property: JsonPropertyName("state")] string State,
    [property: JsonPropertyName("country")] string Country,
    [property: JsonPropertyName("zip_code")] string? ZipCode,
    [property: JsonPropertyName("address")] string? Address,
    [property: JsonPropertyName("bedrooms")] int Bedrooms,
    [property: JsonPropertyName("bathrooms")] int Bathrooms,
    [property: JsonPropertyName("parking_spaces")] int? ParkingSpaces,
    [property: JsonPropertyName("area_sq_meters")] decimal? AreaSqMeters,
    [property: JsonPropertyName("property_type")] PropertyType PropertyType,
    [property: JsonPropertyName("listing_type")] ListingType ListingType,
    [property: JsonPropertyName("status")] PropertyStatus Status,
    [property: JsonPropertyName("attributes")] Dictionary<string, object>? Attributes,
    [property: JsonPropertyName("images")] IReadOnlyList<PropertyImageResponse> Images,
    [property: JsonPropertyName("contact_phone")] string? ContactPhone = null,
    [property: JsonPropertyName("neighbourhood")] string? Neighbourhood = null
);

public sealed record FeaturedPropertyResponse(
    [property: JsonPropertyName("property")] PropertyResponse Property,
    [property: JsonPropertyName("tenant_slug")] string TenantSlug,
    [property: JsonPropertyName("tenant_name")] string TenantName,
    [property: JsonPropertyName("tenant_logo_url")] string? TenantLogoUrl
);

public sealed record PropertyImageResponse(
    [property: JsonPropertyName("key")] string Key,
    [property: JsonPropertyName("url")] string? Url,
    [property: JsonPropertyName("thumbnail_url")] string? ThumbnailUrl,
    [property: JsonPropertyName("medium_url")] string? MediumUrl,
    [property: JsonPropertyName("order")] int Order,
    [property: JsonPropertyName("is_processed")] bool IsProcessed,
    [property: JsonPropertyName("original_file_name")] string OriginalFileName
);
