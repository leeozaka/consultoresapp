namespace Homeless.Domain.ValueObjects;

/// <summary>
/// Immutable value object for tenant branding configuration.
/// Enables white-labeling: each agency portal has its own look-and-feel.
/// Stored as JSONB on the Tenant entity.
/// </summary>
public sealed record BrandingConfig
{
    /// <summary>Cloudflare R2 / CDN URL of the agency logo.</summary>
    public string? LogoUrl { get; init; }

    /// <summary>Primary brand color (CSS hex, e.g. #1A73E8).</summary>
    public string PrimaryColor { get; init; } = "#1A73E8";

    /// <summary>Secondary / accent color.</summary>
    public string SecondaryColor { get; init; } = "#F5A623";

    /// <summary>Display name shown on the portal header.</summary>
    public string AgencyDisplayName { get; init; } = string.Empty;

    /// <summary>Optional tagline / slogan.</summary>
    public string? Tagline { get; init; }

    /// <summary>Favicon URL.</summary>
    public string? FaviconUrl { get; init; }

    /// <summary>Hero, listing copy, and extra imagery for Minimal / Premium portal shells.</summary>
    public PortalBrandingContent? PortalContent { get; init; }

    public static BrandingConfig Default(string agencyName) =>
        new() { AgencyDisplayName = agencyName };
}
