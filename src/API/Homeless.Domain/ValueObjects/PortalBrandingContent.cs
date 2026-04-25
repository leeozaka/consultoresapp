namespace Homeless.Domain.ValueObjects;

/// <summary>
/// Optional rich content for platform-hosted portals (Minimal / Premium themes).
/// Stored inside <see cref="BrandingConfig"/> JSONB.
/// </summary>
public sealed record PortalBrandingContent
{
    public string? HeroImageUrl { get; init; }
    public string? HeroHeadline { get; init; }
    public string? HeroSubhead { get; init; }
    public string? HeroCtaLabel { get; init; }
    public string? HeroCtaUrl { get; init; }
    public string? ListingIntro { get; init; }
    public string? SecondaryHeroImageUrl { get; init; }

    // Trust bar / stats (Minimalist theme)
    public string? Stat1Label { get; init; }
    public string? Stat1Value { get; init; }
    public string? Stat2Label { get; init; }
    public string? Stat2Value { get; init; }

    // Feature highlight cards (Premium theme — 3 cards below hero)
    public string? FeatureCard1Title { get; init; }
    public string? FeatureCard1Description { get; init; }
    public string? FeatureCard1Icon { get; init; }
    public string? FeatureCard2Title { get; init; }
    public string? FeatureCard2Description { get; init; }
    public string? FeatureCard2Icon { get; init; }
    public string? FeatureCard3Title { get; init; }
    public string? FeatureCard3Description { get; init; }
    public string? FeatureCard3Icon { get; init; }

    // WhatsApp floating button
    public string? WhatsappNumber { get; init; }

    // Secondary CTA (Premium hero)
    public string? HeroSecondaryCta { get; init; }
}
