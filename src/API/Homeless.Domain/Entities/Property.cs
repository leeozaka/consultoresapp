using Homeless.Domain.Enums;
using Homeless.Domain.Events;
using Homeless.Domain.Exceptions;
using Homeless.Domain.ValueObjects;

namespace Homeless.Domain.Entities;

/// <summary>
/// Property aggregate root. Represents a real estate listing owned by a tenant.
/// Uses a hybrid SQL/JSONB model: indexed columns for filtering, JSONB for flexible attributes.
/// </summary>
public sealed class Property : TenantEntity
{
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public Money Price { get; private set; } = Money.Zero(Currency.BRL);

    public string City { get; private set; } = string.Empty;
    public string State { get; private set; } = string.Empty;
    public string Country { get; private set; } = "BR";
    public string? ZipCode { get; private set; }
    public string? Address { get; private set; }
    public string? Neighbourhood { get; private set; }
    public int Bedrooms { get; private set; }
    public int Bathrooms { get; private set; }
    public int? ParkingSpaces { get; private set; }
    public decimal? AreaSqMeters { get; private set; }

    public PropertyType PropertyType { get; private set; }
    public ListingType ListingType { get; private set; }
    public PropertyStatus Status { get; private set; } = PropertyStatus.Draft;

    // ── Flexible attributes (JSONB with GIN index) ─────────────────────────
    /// <summary>
    /// Free-form amenities and tags. Examples: {"HasPool":true,"HasElevator":true,"FloorNumber":5}.
    /// Queried via: Attributes @> '{"HasPool":true}'.
    /// </summary>
    public Dictionary<string, object> Attributes { get; private set; } = [];

    // ── Images (JSONB array) ──────────────────────────────────────────────────
    /// <summary>
    /// EF Core maps this directly. The public interface returns a read-only view.
    /// Domain methods mutate via the private setter.
    /// </summary>
    public List<PropertyImage> Images { get; private set; } = [];

    /// <summary>
    /// WhatsApp contact number shown on the portal listing.
    /// Only populated when the tenant has the <c>whatsapp_button</c> perk active.
    /// </summary>
    public string? ContactPhone { get; private set; }

    /// <summary>The user (Agent or TenantAdmin) responsible for this listing.</summary>
    public Guid? AgentId { get; private set; }

    public DateTime? PublishedAtUtc { get; private set; }

    private Property() { }

    public static Property Create(
        Guid tenantId,
        string title,
        Money price,
        string city,
        string state,
        PropertyType propertyType,
        ListingType listingType,
        int bedrooms,
        int bathrooms,
        Guid? agentId = null
    )
    {
        var property = new Property
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Title = title.Trim(),
            Price = price,
            City = city.Trim(),
            State = state.Trim(),
            PropertyType = propertyType,
            ListingType = listingType,
            Bedrooms = bedrooms,
            Bathrooms = bathrooms,
            AgentId = agentId,
            Status = PropertyStatus.Draft,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        property.RaiseDomainEvent(new PropertyCreatedEvent(property.Id, tenantId, title));

        return property;
    }

    public void UpdateDetails(
        string title,
        string? description,
        Money price,
        string city,
        string state,
        PropertyType propertyType,
        ListingType listingType,
        string? zipCode,
        string? address,
        int bedrooms,
        int bathrooms,
        int? parkingSpaces,
        decimal? areaSqMeters,
        Dictionary<string, object>? attributes,
        string? contactPhone = null,
        string? neighbourhood = null
    )
    {
        EnsureNotArchived();

        Title = title.Trim();
        Description = description?.Trim();
        Price = price;
        City = city.Trim();
        State = state.Trim();
        PropertyType = propertyType;
        ListingType = listingType;
        ZipCode = zipCode?.Trim();
        Address = address?.Trim();
        Neighbourhood = neighbourhood?.Trim();
        Bedrooms = bedrooms;
        Bathrooms = bathrooms;
        ParkingSpaces = parkingSpaces;
        AreaSqMeters = areaSqMeters;
        ContactPhone = contactPhone?.Trim();
        if (attributes != null)
            Attributes = attributes;
    }

    public void Publish()
    {
        if (Status == PropertyStatus.Archived)
            throw new DomainException("PROPERTY_ARCHIVED", "Cannot publish an archived property.");

        if (string.IsNullOrWhiteSpace(Title) || string.IsNullOrWhiteSpace(City))
            throw new DomainException(
                "PROPERTY_INCOMPLETE",
                "Title and City are required before publishing."
            );

        Status = PropertyStatus.Active;
        PublishedAtUtc ??= DateTime.UtcNow;
        RaiseDomainEvent(new PropertyPublishedEvent(Id, TenantId));
    }

    public void Archive()
    {
        Status = PropertyStatus.Archived;
    }

    public void Deactivate()
    {
        if (Status != PropertyStatus.Active)
            throw new DomainException(
                "PROPERTY_NOT_ACTIVE",
                "Only active properties can be deactivated."
            );

        Status = PropertyStatus.Draft;
    }

    public void MarkAsSold()
    {
        if (Status != PropertyStatus.Active && Status != PropertyStatus.UnderOffer)
            throw new DomainException(
                "PROPERTY_NOT_ACTIVE",
                "Only active or under-offer properties can be marked as sold."
            );

        Status = PropertyStatus.Sold;
    }

    public void MarkAsRented()
    {
        if (Status != PropertyStatus.Active && Status != PropertyStatus.UnderOffer)
            throw new DomainException(
                "PROPERTY_NOT_ACTIVE",
                "Only active or under-offer properties can be marked as rented."
            );

        Status = PropertyStatus.Rented;
    }

    public void SetAgent(Guid agentId)
    {
        AgentId = agentId;
    }

    public void AddImage(string key, string originalFileName)
    {
        EnsureNotArchived();
        var order = Images.Count;
        var image = PropertyImage.CreatePending(key, originalFileName, order);
        Images.Add(image);
        RaiseDomainEvent(new PropertyImageUploadedEvent(Id, TenantId, key, originalFileName));
    }

    public void MarkImageProcessed(string key, string url, string thumbnailUrl, string mediumUrl)
    {
        var index = Images.FindIndex(i => i.Key == key);
        if (index < 0)
            throw new DomainException("IMAGE_NOT_FOUND", $"Image with key '{key}' not found.");

        Images[index] = Images[index].MarkProcessed(url, thumbnailUrl, mediumUrl);
    }

    public void RemoveImage(string key)
    {
        EnsureNotArchived();
        var index = Images.FindIndex(i => i.Key == key);
        if (index < 0)
            throw new DomainException("IMAGE_NOT_FOUND", $"Image with key '{key}' not found.");

        Images.RemoveAt(index);
        // Re-order remaining images
        for (var i = 0; i < Images.Count; i++)
            Images[i] = Images[i] with { Order = i };
    }

    private void EnsureNotArchived()
    {
        if (Status == PropertyStatus.Archived)
            throw new DomainException(
                "PROPERTY_ARCHIVED",
                "Archived properties cannot be modified."
            );
    }
}
