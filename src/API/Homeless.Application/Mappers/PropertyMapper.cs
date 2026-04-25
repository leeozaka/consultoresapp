using Homeless.Application.DTOs;
using Homeless.Application.Interfaces;
using Homeless.Domain.Entities;
using Homeless.Domain.ValueObjects;
using Riok.Mapperly.Abstractions;

namespace Homeless.Application.Mappers;

[Mapper]
public static partial class PropertyMapper
{
    /// <summary>Maps a search DTO to the domain <see cref="PropertyFilter"/> value object.</summary>
    public static PropertyFilter ToFilter(this PropertySearchRequest r) =>
        new(r.City, r.Neighbourhood, r.MinPrice, r.MaxPrice, r.MinBedrooms, r.PropertyType, r.ListingType, r.Status);

    /// <summary>Extracts the shared mutable fields from a create request into <see cref="PropertyWriteData"/>.</summary>
    public static PropertyWriteData ToWriteData(this CreatePropertyRequest r) =>
        new(r.Title, r.Price, r.City, r.State, r.PropertyType, r.ListingType,
            r.Bedrooms, r.Bathrooms, r.Description, r.ZipCode, r.Address,
            r.ParkingSpaces, r.AreaSqMeters, r.Attributes, r.ContactPhone, r.Neighbourhood);

    /// <summary>Extracts the shared mutable fields from an update request into <see cref="PropertyWriteData"/>.</summary>
    public static PropertyWriteData ToWriteData(this UpdatePropertyRequest r) =>
        new(r.Title, r.Price, r.City, r.State, r.PropertyType, r.ListingType,
            r.Bedrooms, r.Bathrooms, r.Description, r.ZipCode, r.Address,
            r.ParkingSpaces, r.AreaSqMeters, r.Attributes, r.ContactPhone, r.Neighbourhood);

    public static PropertyResponse ToResponse(this Property property) =>
        new(
            property.Id,
            property.Title,
            property.Description,
            property.Price.AmountInCents / 100m,
            property.Price.Currency.Code,
            property.City,
            property.State,
            property.Country,
            property.ZipCode,
            property.Address,
            property.Bedrooms,
            property.Bathrooms,
            property.ParkingSpaces,
            property.AreaSqMeters,
            property.PropertyType,
            property.ListingType,
            property.Status,
            property.Attributes,
            property.Images.Select(img => img.ToResponse()).ToList().AsReadOnly(),
            property.ContactPhone,
            property.Neighbourhood);

    public static partial PropertyImageResponse ToResponse(this PropertyImage image);

    /// <summary>
    /// For images that haven't been processed yet (no URL), falls back to the raw S3 key's
    /// public URL so the browser can display the original upload immediately.
    /// </summary>
    public static PropertyResponse WithRawImageUrls(this PropertyResponse response, IStorageService storage)
    {
        if (response.Images.All(i => i.IsProcessed))
            return response;

        var patched = response.Images.Select(img =>
            img.IsProcessed || img.Url is not null
                ? img
                : img with { Url = storage.GetPublicUrl(img.Key) }
        ).ToList().AsReadOnly();

        return response with { Images = patched };
    }

}
