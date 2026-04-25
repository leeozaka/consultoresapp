using Homeless.Application.DTOs;
using Homeless.Domain.Enums;
using Homeless.Domain.ValueObjects;

namespace Homeless.Infrastructure.Persistence.Repositories.Read;

/// <summary>
/// Lightweight read model for property queries.
/// This is intentionally projected manually in LINQ instead of using Mapperly IQueryable projection,
/// because EF Core / Npgsql cannot translate nested DTO construction over the JSONB-backed
/// <see cref="PropertyImage"/> collection.
/// </summary>
public sealed record PropertyReadProjection(
    Guid Id,
    string Title,
    string? Description,
    long PriceCents,
    string PriceCurrency,
    string City,
    string State,
    string Country,
    string? ZipCode,
    string? Address,
    int Bedrooms,
    int Bathrooms,
    int? ParkingSpaces,
    decimal? AreaSqMeters,
    PropertyType PropertyType,
    ListingType ListingType,
    PropertyStatus Status,
    Dictionary<string, object>? Attributes,
    List<PropertyImage> Images,
    string? ContactPhone = null,
    string? Neighbourhood = null
);

internal sealed record FeaturedReadProjection(Guid TenantId, PropertyReadProjection Projection);

internal sealed record FeaturedPropertyReadProjection(
    PropertyReadProjection Property,
    string TenantSlug,
    string TenantName,
    BrandingConfig Branding,
    DateTime SortDate);

public static class PropertyReadProjectionMapper
{
    public static PropertyResponse ToResponse(this PropertyReadProjection p)
        => new(
            p.Id,
            p.Title,
            p.Description,
            p.PriceCents / 100m,
            p.PriceCurrency,
            p.City,
            p.State,
            p.Country,
            p.ZipCode,
            p.Address,
            p.Bedrooms,
            p.Bathrooms,
            p.ParkingSpaces,
            p.AreaSqMeters,
            p.PropertyType,
            p.ListingType,
            p.Status,
            p.Attributes,
            p.Images.Select(i => new PropertyImageResponse(
                i.Key,
                i.Url,
                i.ThumbnailUrl,
                i.MediumUrl,
                i.Order,
                i.IsProcessed,
                i.OriginalFileName)).ToList().AsReadOnly(),
            p.ContactPhone,
            p.Neighbourhood);

    internal static FeaturedPropertyResponse ToResponse(this FeaturedPropertyReadProjection p)
        => new(
            p.Property.ToResponse(),
            p.TenantSlug,
            p.TenantName,
            p.Branding.LogoUrl);
}
