using Microsoft.EntityFrameworkCore;
using Homeless.Application.DTOs;
using Homeless.Application.Interfaces;
using Homeless.Domain.Entities;
using Homeless.Domain.Enums;
using Homeless.Domain.ValueObjects;
using Homeless.Infrastructure.Persistence.Context;

namespace Homeless.Infrastructure.Persistence.Repositories.Read;

/// <summary>
/// Query service for <see cref="DTOs.PropertyResponse"/>.
/// Uses a manual SQL projection into <see cref="PropertyReadProjection"/>.
/// The JSONB-backed <see cref="Property.Images"/> column is selected as raw data and only the nested
/// <see cref="PropertyImageResponse"/> mapping happens in memory, because EF Core / Npgsql cannot translate
/// nested DTO construction over that JSONB collection.
/// <see cref="AccountQueryService"/> still uses full Mapperly IQueryable projection.
/// </summary>
public sealed class PropertyQueryService(ReadDbContext context) : IPropertyQueryService
{
    public async Task<PropertyResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await Project(context.Properties
            .Where(p => p.Id == id))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return item?.ToResponse();
    }

    public async Task<(IReadOnlyList<PropertyResponse> Items, int TotalCount)> SearchAsync(
        Guid tenantId,
        PropertyFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = context.Properties
            .Where(p => p.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(filter.City))
            query = query.Where(p => p.City.ToLower().Contains(filter.City.ToLower()));

        if (!string.IsNullOrWhiteSpace(filter.Neighbourhood))
            query = query.Where(p => p.Neighbourhood != null &&
                p.Neighbourhood.ToLower().Contains(filter.Neighbourhood.ToLower()));

        if (filter.MinPrice.HasValue) query = query.Where(p => p.Price.AmountInCents >= (long)(filter.MinPrice.Value * 100));
        if (filter.MaxPrice.HasValue) query = query.Where(p => p.Price.AmountInCents <= (long)(filter.MaxPrice.Value * 100));
        if (filter.MinBedrooms.HasValue) query = query.Where(p => p.Bedrooms >= filter.MinBedrooms.Value);
        if (filter.PropertyType.HasValue) query = query.Where(p => p.PropertyType == filter.PropertyType.Value);
        if (filter.ListingType.HasValue) query = query.Where(p => p.ListingType == filter.ListingType.Value);
        if (filter.Status.HasValue)
            query = query.Where(p => p.Status == filter.Status.Value);
        else
            query = query.Where(p => p.Status != PropertyStatus.Archived);

        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        var items = await Project(query
            .OrderByDescending(p => p.PublishedAtUtc ?? p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (items.Select(p => p.ToResponse()).ToList().AsReadOnly(), totalCount);
    }

    public async Task<(IReadOnlyList<FeaturedPropertyResponse> Items, int TotalCount)> GetFeaturedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var activeTenantIds = context.Tenants
            .IgnoreQueryFilters()
            .Where(t => t.Status == TenantStatus.Active)
            .Select(t => t.Id);

        var query =
            from p in context.Properties.IgnoreQueryFilters()
            join t in context.Tenants.IgnoreQueryFilters().AsNoTracking()
                on p.TenantId equals t.Id
            where p.Status == PropertyStatus.Active
            where activeTenantIds.Contains(p.TenantId)
            select new
            {
                Property = p,
                TenantSlug = t.Slug,
                TenantName = t.Name,
                Branding = t.Branding,
                SortDate = p.PublishedAtUtc ?? p.CreatedAt
            };

        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        var items = await query
            .OrderByDescending(p => p.SortDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new FeaturedPropertyReadProjection(
                new PropertyReadProjection(
                    x.Property.Id,
                    x.Property.Title,
                    x.Property.Description,
                    x.Property.Price.AmountInCents,
                    x.Property.Price.Currency.Code,
                    x.Property.City,
                    x.Property.State,
                    x.Property.Country,
                    x.Property.ZipCode,
                    x.Property.Address,
                    x.Property.Bedrooms,
                    x.Property.Bathrooms,
                    x.Property.ParkingSpaces,
                    x.Property.AreaSqMeters,
                    x.Property.PropertyType,
                    x.Property.ListingType,
                    x.Property.Status,
                    x.Property.Attributes,
                    x.Property.Images,
                    x.Property.ContactPhone,
                    x.Property.Neighbourhood),
                x.TenantSlug,
                x.TenantName,
                x.Branding,
                x.SortDate))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (items.Select(p => p.ToResponse()).ToList().AsReadOnly(), totalCount);
    }

    private static IQueryable<PropertyReadProjection> Project(IQueryable<Property> query)
        => query.Select(p => new PropertyReadProjection(
            p.Id,
            p.Title,
            p.Description,
            p.Price.AmountInCents,
            p.Price.Currency.Code,
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
            p.Images,
            p.ContactPhone,
            p.Neighbourhood));

    public async Task<IReadOnlyList<LocationSuggestionResponse>> GetLocationSuggestionsAsync(
        Guid tenantId, string? query, CancellationToken cancellationToken = default)
    {
        var baseQ = context.Properties
            .Where(p => p.TenantId == tenantId && p.Status == PropertyStatus.Active);

        // Properties whose city name matches the search term.
        var cityMatchQ = baseQ;
        // Properties whose neighbourhood name matches the search term.
        var nhoodMatchQ = baseQ.Where(_ => false); // empty by default

        if (!string.IsNullOrWhiteSpace(query))
        {
            var lower = query.ToLower();
            cityMatchQ  = baseQ.Where(p => p.City.ToLower().Contains(lower));
            nhoodMatchQ = baseQ.Where(p => p.Neighbourhood != null && p.Neighbourhood.ToLower().Contains(lower));
        }

        var cities = await cityMatchQ
            .Select(p => new { p.City, p.State })
            .Distinct()
            .Take(5)
            .Select(x => new LocationSuggestionResponse(x.City, "city", x.City, x.State))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // Collect the cities touched by either match strategy:
        //   • city-name match (e.g. "Presidente" → "Presidente Prudente")
        //   • neighbourhood-name match (e.g. "Jardim" → the city that owns "Jardim Cobral")
        // Then return ALL neighbourhoods from those cities, so searching "Jardim"
        // surfaces every neighbourhood in Presidente Prudente — not only the ones
        // literally named "Jardim".
        var matchingCities = cityMatchQ.Select(p => p.City)
            .Union(nhoodMatchQ.Select(p => p.City));

        var neighbourhoods = await baseQ
            .Where(p => p.Neighbourhood != null && matchingCities.Contains(p.City))
            .Select(p => new { p.Neighbourhood, p.City, p.State })
            .Distinct()
            .Take(8)
            .Select(x => new LocationSuggestionResponse(x.Neighbourhood!, "neighbourhood", x.City, x.State))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return [..cities, ..neighbourhoods];
    }
}
