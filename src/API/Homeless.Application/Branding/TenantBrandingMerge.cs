using Homeless.Application.DTOs;
using Homeless.Domain.ValueObjects;

namespace Homeless.Application.Branding;

public static class TenantBrandingMerge
{
    public static BrandingConfig MergeFromAdminRequest(BrandingConfig current, UpdateTenantBrandingRequest request)
    {
        var portal = MergePortalContent(current.PortalContent, request.PortalContent);

        return current with
        {
            LogoUrl = PatchOptional(request.LogoUrl, current.LogoUrl),
            PrimaryColor = request.PrimaryColor.Trim(),
            SecondaryColor = request.SecondaryColor.Trim(),
            AgencyDisplayName = request.AgencyDisplayName.Trim(),
            Tagline = PatchOptional(request.Tagline, current.Tagline),
            FaviconUrl = PatchOptional(request.FaviconUrl, current.FaviconUrl),
            PortalContent = portal
        };
    }

    public static BrandingConfig MergeFromTenantSelfService(
        BrandingConfig current,
        string primaryColor,
        string secondaryColor,
        string agencyDisplayName,
        string? tagline,
        string? logoUrl,
        string? faviconUrl,
        PortalBrandingContent? portalPatch)
    {
        var portal = portalPatch is null
            ? current.PortalContent
            : MergePortalContentDomain(current.PortalContent, portalPatch);

        return current with
        {
            LogoUrl = PatchOptional(logoUrl, current.LogoUrl),
            PrimaryColor = primaryColor.Trim(),
            SecondaryColor = secondaryColor.Trim(),
            AgencyDisplayName = agencyDisplayName.Trim(),
            Tagline = PatchOptional(tagline, current.Tagline),
            FaviconUrl = PatchOptional(faviconUrl, current.FaviconUrl),
            PortalContent = portal
        };
    }

    private static PortalBrandingContent? MergePortalContent(
        PortalBrandingContent? current,
        PortalBrandingContentResponse? patch)
    {
        if (patch is null)
            return current;

        var prev = current ?? new PortalBrandingContent();
        return new PortalBrandingContent
        {
            HeroImageUrl = PatchOptional(patch.HeroImageUrl, prev.HeroImageUrl),
            HeroHeadline = PatchOptional(patch.HeroHeadline, prev.HeroHeadline),
            HeroSubhead = PatchOptional(patch.HeroSubhead, prev.HeroSubhead),
            HeroCtaLabel = PatchOptional(patch.HeroCtaLabel, prev.HeroCtaLabel),
            HeroCtaUrl = PatchOptional(patch.HeroCtaUrl, prev.HeroCtaUrl),
            ListingIntro = PatchOptional(patch.ListingIntro, prev.ListingIntro),
            SecondaryHeroImageUrl = PatchOptional(patch.SecondaryHeroImageUrl, prev.SecondaryHeroImageUrl),
            Stat1Label = PatchOptional(patch.Stat1Label, prev.Stat1Label),
            Stat1Value = PatchOptional(patch.Stat1Value, prev.Stat1Value),
            Stat2Label = PatchOptional(patch.Stat2Label, prev.Stat2Label),
            Stat2Value = PatchOptional(patch.Stat2Value, prev.Stat2Value),
            FeatureCard1Title = PatchOptional(patch.FeatureCard1Title, prev.FeatureCard1Title),
            FeatureCard1Description = PatchOptional(patch.FeatureCard1Description, prev.FeatureCard1Description),
            FeatureCard1Icon = PatchOptional(patch.FeatureCard1Icon, prev.FeatureCard1Icon),
            FeatureCard2Title = PatchOptional(patch.FeatureCard2Title, prev.FeatureCard2Title),
            FeatureCard2Description = PatchOptional(patch.FeatureCard2Description, prev.FeatureCard2Description),
            FeatureCard2Icon = PatchOptional(patch.FeatureCard2Icon, prev.FeatureCard2Icon),
            FeatureCard3Title = PatchOptional(patch.FeatureCard3Title, prev.FeatureCard3Title),
            FeatureCard3Description = PatchOptional(patch.FeatureCard3Description, prev.FeatureCard3Description),
            FeatureCard3Icon = PatchOptional(patch.FeatureCard3Icon, prev.FeatureCard3Icon),
            WhatsappNumber = PatchOptional(patch.WhatsappNumber, prev.WhatsappNumber),
            HeroSecondaryCta = PatchOptional(patch.HeroSecondaryCta, prev.HeroSecondaryCta)
        };
    }

    private static PortalBrandingContent? MergePortalContentDomain(
        PortalBrandingContent? current,
        PortalBrandingContent patch)
    {
        var prev = current ?? new PortalBrandingContent();
        return new PortalBrandingContent
        {
            HeroImageUrl = PatchOptional(patch.HeroImageUrl, prev.HeroImageUrl),
            HeroHeadline = PatchOptional(patch.HeroHeadline, prev.HeroHeadline),
            HeroSubhead = PatchOptional(patch.HeroSubhead, prev.HeroSubhead),
            HeroCtaLabel = PatchOptional(patch.HeroCtaLabel, prev.HeroCtaLabel),
            HeroCtaUrl = PatchOptional(patch.HeroCtaUrl, prev.HeroCtaUrl),
            ListingIntro = PatchOptional(patch.ListingIntro, prev.ListingIntro),
            SecondaryHeroImageUrl = PatchOptional(patch.SecondaryHeroImageUrl, prev.SecondaryHeroImageUrl),
            Stat1Label = PatchOptional(patch.Stat1Label, prev.Stat1Label),
            Stat1Value = PatchOptional(patch.Stat1Value, prev.Stat1Value),
            Stat2Label = PatchOptional(patch.Stat2Label, prev.Stat2Label),
            Stat2Value = PatchOptional(patch.Stat2Value, prev.Stat2Value),
            FeatureCard1Title = PatchOptional(patch.FeatureCard1Title, prev.FeatureCard1Title),
            FeatureCard1Description = PatchOptional(patch.FeatureCard1Description, prev.FeatureCard1Description),
            FeatureCard1Icon = PatchOptional(patch.FeatureCard1Icon, prev.FeatureCard1Icon),
            FeatureCard2Title = PatchOptional(patch.FeatureCard2Title, prev.FeatureCard2Title),
            FeatureCard2Description = PatchOptional(patch.FeatureCard2Description, prev.FeatureCard2Description),
            FeatureCard2Icon = PatchOptional(patch.FeatureCard2Icon, prev.FeatureCard2Icon),
            FeatureCard3Title = PatchOptional(patch.FeatureCard3Title, prev.FeatureCard3Title),
            FeatureCard3Description = PatchOptional(patch.FeatureCard3Description, prev.FeatureCard3Description),
            FeatureCard3Icon = PatchOptional(patch.FeatureCard3Icon, prev.FeatureCard3Icon),
            WhatsappNumber = PatchOptional(patch.WhatsappNumber, prev.WhatsappNumber),
            HeroSecondaryCta = PatchOptional(patch.HeroSecondaryCta, prev.HeroSecondaryCta)
        };
    }

    private static string? PatchOptional(string? incoming, string? previous)
    {
        if (incoming is null)
            return previous;

        return string.IsNullOrWhiteSpace(incoming) ? null : incoming.Trim();
    }
}
