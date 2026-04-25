using Ardalis.Result;
using MediatR;
using Homeless.Application.Branding;
using Homeless.Application.DTOs;
using Homeless.Application.Interfaces;
using Homeless.Application.Mappers;
using Homeless.Domain.Interfaces.Repositories.Read;
using Homeless.Domain.Interfaces.Repositories.Write;
using Homeless.Domain.ValueObjects;

namespace Homeless.Application.UseCases.Tenants;

public sealed class UpdateCurrentTenantBrandingHandler(
    ITenantReadRepository tenantReadRepository,
    ITenantWriteRepository tenantWriteRepository,
    IUnitOfWork unitOfWork,
    ITenantContext tenantContext)
    : IRequestHandler<UpdateCurrentTenantBrandingCommand, Result<TenantResponse>>
{
    public async Task<Result<TenantResponse>> Handle(
        UpdateCurrentTenantBrandingCommand request,
        CancellationToken cancellationToken)
    {
        if (!tenantContext.IsResolved)
            return Result.NotFound("No tenant resolved for this request.");

        var tenant = await tenantReadRepository
            .GetByIdAsync(tenantContext.TenantId, cancellationToken)
            .ConfigureAwait(false);

        if (tenant is null)
            return Result.NotFound($"Tenant '{tenantContext.TenantId}' not found.");

        if (tenant.FrontendOrigin is { Length: > 0 })
            return Result.Forbidden();

        var theme = tenant.PortalTheme;
        var validation = ValidateRequestForTheme(theme, request.Request);
        if (validation is not null)
            return validation;

        var merged = TenantBrandingMerge.MergeFromTenantSelfService(
            tenant.Branding,
            request.Request.PrimaryColor,
            request.Request.SecondaryColor,
            request.Request.AgencyDisplayName,
            request.Request.Tagline,
            request.Request.LogoUrl,
            request.Request.FaviconUrl,
            MapPortalPatch(request.Request.PortalContent));

        if (theme == "default")
            merged = merged with { PortalContent = null };
        else if (theme == "minimal" && merged.PortalContent is { } pc)
            merged = merged with
            {
                PortalContent = pc with
                {
                    SecondaryHeroImageUrl = null,
                    FeatureCard1Title = null, FeatureCard1Description = null, FeatureCard1Icon = null,
                    FeatureCard2Title = null, FeatureCard2Description = null, FeatureCard2Icon = null,
                    FeatureCard3Title = null, FeatureCard3Description = null, FeatureCard3Icon = null,
                    HeroSecondaryCta = null
                }
            };

        tenant.UpdateBranding(merged);

        await tenantWriteRepository.UpdateAsync(tenant, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(tenant.ToResponse());
    }

    private static Result? ValidateRequestForTheme(string theme, UpdateTenantBrandingRequest body)
    {
        var p = body.PortalContent;
        if (p is null)
            return null;

        // TODO: fix stale validations to fluent
        var hasSecondary = !string.IsNullOrWhiteSpace(p.SecondaryHeroImageUrl);
        var hasStats = !string.IsNullOrWhiteSpace(p.Stat1Label)
            || !string.IsNullOrWhiteSpace(p.Stat1Value)
            || !string.IsNullOrWhiteSpace(p.Stat2Label)
            || !string.IsNullOrWhiteSpace(p.Stat2Value);
        var hasFeatureCards = !string.IsNullOrWhiteSpace(p.FeatureCard1Title)
            || !string.IsNullOrWhiteSpace(p.FeatureCard1Description)
            || !string.IsNullOrWhiteSpace(p.FeatureCard1Icon)
            || !string.IsNullOrWhiteSpace(p.FeatureCard2Title)
            || !string.IsNullOrWhiteSpace(p.FeatureCard2Description)
            || !string.IsNullOrWhiteSpace(p.FeatureCard2Icon)
            || !string.IsNullOrWhiteSpace(p.FeatureCard3Title)
            || !string.IsNullOrWhiteSpace(p.FeatureCard3Description)
            || !string.IsNullOrWhiteSpace(p.FeatureCard3Icon);
        var hasSecondaryCta = !string.IsNullOrWhiteSpace(p.HeroSecondaryCta);
        var hasWhatsapp = !string.IsNullOrWhiteSpace(p.WhatsappNumber);
        var hasRich = hasSecondary
            || !string.IsNullOrWhiteSpace(p.HeroImageUrl)
            || !string.IsNullOrWhiteSpace(p.HeroHeadline)
            || !string.IsNullOrWhiteSpace(p.HeroSubhead)
            || !string.IsNullOrWhiteSpace(p.HeroCtaLabel)
            || !string.IsNullOrWhiteSpace(p.HeroCtaUrl)
            || !string.IsNullOrWhiteSpace(p.ListingIntro)
            || hasStats
            || hasFeatureCards
            || hasSecondaryCta
            || hasWhatsapp;

        if (theme == "default" && hasRich)
            return Result.Invalid(new ValidationError("Portal content fields require a higher plan."));

        if (theme == "minimal")
        {
            if (hasSecondary)
                return Result.Invalid(new ValidationError("Secondary hero image is available on the Premium plan only."));
            if (hasFeatureCards)
                return Result.Invalid(new ValidationError("Feature cards are available on the Premium plan only."));
            if (hasSecondaryCta)
                return Result.Invalid(new ValidationError("Secondary CTA is available on the Premium plan only."));
        }

        return null;
    }

    private static PortalBrandingContent? MapPortalPatch(PortalBrandingContentResponse? p)
    {
        if (p is null)
            return null;

        return new PortalBrandingContent
        {
            HeroImageUrl = p.HeroImageUrl,
            HeroHeadline = p.HeroHeadline,
            HeroSubhead = p.HeroSubhead,
            HeroCtaLabel = p.HeroCtaLabel,
            HeroCtaUrl = p.HeroCtaUrl,
            ListingIntro = p.ListingIntro,
            SecondaryHeroImageUrl = p.SecondaryHeroImageUrl,
            Stat1Label = p.Stat1Label,
            Stat1Value = p.Stat1Value,
            Stat2Label = p.Stat2Label,
            Stat2Value = p.Stat2Value,
            FeatureCard1Title = p.FeatureCard1Title,
            FeatureCard1Description = p.FeatureCard1Description,
            FeatureCard1Icon = p.FeatureCard1Icon,
            FeatureCard2Title = p.FeatureCard2Title,
            FeatureCard2Description = p.FeatureCard2Description,
            FeatureCard2Icon = p.FeatureCard2Icon,
            FeatureCard3Title = p.FeatureCard3Title,
            FeatureCard3Description = p.FeatureCard3Description,
            FeatureCard3Icon = p.FeatureCard3Icon,
            WhatsappNumber = p.WhatsappNumber,
            HeroSecondaryCta = p.HeroSecondaryCta
        };
    }
}
