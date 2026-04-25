using System.Diagnostics.CodeAnalysis;
using Homeless.Domain.ValueObjects;

namespace Homeless.Domain.Entities;

/// <summary>
/// Subscription plan offered by the platform. Managed by SuperAdmin.
/// Defines the base capacity limits and feature entitlements a tenant receives
/// when subscribed to this plan.
/// </summary>
public sealed class Plan : Entity
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public Money Price { get; private set; } = Money.Zero(Currency.BRL);
    public string? StripePriceId { get; private set; }

    public int MaxProperties { get; private set; }
    public bool VideoUpload { get; private set; }
    public bool AiDescriptions { get; private set; }
    public bool CustomDomain { get; private set; }
    public bool PremiumAnalytics { get; private set; }

    /// <summary>
    /// Drives <c>portal_theme</c> base entitlement: <c>default</c>, <c>minimal</c>, or <c>premium</c>.
    /// </summary>
    public string PortalTheme { get; private set; } = "default";

    public bool IsActive { get; private set; } = true;

    private Plan() { }

    [SuppressMessage(
        "Maintainability",
        "S107:Methods should not have too many parameters",
        Justification = "Plan creation needs to capture full pricing and entitlement configuration in one aggregate factory.")]
    public static Plan Create(
        string name,
        string description,
        Money price,
        int maxProperties,
        bool videoUpload = false,
        bool aiDescriptions = false,
        bool customDomain = false,
        bool premiumAnalytics = false,
        string? stripePriceId = null,
        string portalTheme = "default")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new Plan
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Description = description.Trim(),
            Price = price,
            MaxProperties = maxProperties,
            VideoUpload = videoUpload,
            AiDescriptions = aiDescriptions,
            CustomDomain = customDomain,
            PremiumAnalytics = premiumAnalytics,
            PortalTheme = NormalizePortalTheme(portalTheme),
            StripePriceId = NormalizeStripePriceId(stripePriceId),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    [SuppressMessage(
        "Maintainability",
        "S107:Methods should not have too many parameters",
        Justification = "Plan updates apply all pricing and entitlement fields atomically on the aggregate.")]
    public void Update(
        string name,
        string description,
        Money price,
        int maxProperties,
        bool videoUpload,
        bool aiDescriptions,
        bool customDomain,
        bool premiumAnalytics,
        string? stripePriceId = null,
        string? portalTheme = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name.Trim();
        Description = description.Trim();
        Price = price;
        MaxProperties = maxProperties;
        VideoUpload = videoUpload;
        AiDescriptions = aiDescriptions;
        CustomDomain = customDomain;
        PremiumAnalytics = premiumAnalytics;
        StripePriceId = NormalizeStripePriceId(stripePriceId);
        if (portalTheme is not null)
            PortalTheme = NormalizePortalTheme(portalTheme);
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void SetStripePriceId(string? stripePriceId)
    {
        StripePriceId = NormalizeStripePriceId(stripePriceId);
    }

    /// <summary>
    /// Produces the entitlements dictionary that should be applied to a tenant
    /// subscribing to this plan. Only base-plan keys are set here; perk add-on
    /// entitlements (whatsapp_button, featured_listings, …) are managed separately
    /// through <see cref="TenantAddon"/> and are NOT overwritten by plan changes.
    /// </summary>
    public Dictionary<string, object> ToBaseEntitlements() => new()
    {
        ["max_properties"] = MaxProperties,
        ["video_upload"]   = VideoUpload,
        ["ai_descriptions"] = AiDescriptions,
        ["custom_domain"]  = CustomDomain,
        ["premium_analytics"] = PremiumAnalytics,
        ["portal_theme"] = PortalTheme,
    };

    private static string? NormalizeStripePriceId(string? stripePriceId) =>
        string.IsNullOrWhiteSpace(stripePriceId) ? null : stripePriceId.Trim();

    private static string NormalizePortalTheme(string value)
    {
        var v = value.Trim().ToLowerInvariant();
        return v switch
        {
            "minimal" => "minimal",
            "premium" => "premium",
            _ => "default"
        };
    }
}
