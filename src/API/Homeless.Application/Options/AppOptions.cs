namespace Homeless.Application.Options;

/// <summary>
/// Top-level application settings shared across the API.
/// </summary>
public sealed class AppOptions
{
    public const string SectionName = "App";

    /// <summary>
    /// The root/landing domain (no subdomain). Used to extract tenant slugs
    /// from subdomain-based routing.
    /// Examples: "consultor.app" (production), "localhost" (local / Docker).
    /// </summary>
    public string LandingDomain { get; init; } = "localhost";

    /// <summary>
    /// Slug of the system landing tenant resolved on the root domain.
    /// </summary>
    public string LandingTenantSlug { get; init; } = "landing";
}
