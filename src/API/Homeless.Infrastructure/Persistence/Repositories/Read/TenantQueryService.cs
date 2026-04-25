using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Homeless.Application.DTOs;
using Homeless.Application.Interfaces;
using Homeless.Domain.Enums;
using Homeless.Domain.ValueObjects;
using Homeless.Infrastructure.Persistence.Context;

namespace Homeless.Infrastructure.Persistence.Repositories.Read;

/// <summary>
/// Query service for <see cref="TenantResponse"/>.
/// Projects to <see cref="TenantReadProjection"/> in SQL (avoiding non-translatable computed properties),
/// then maps to <see cref="TenantResponse"/> in memory.
/// </summary>
public sealed class TenantQueryService(ReadDbContext context) : ITenantQueryService
{
    public async Task<TenantResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await Project(context.Tenants.Where(t => t.Id == id))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return item?.ToResponse();
    }

    public async Task<TenantResponse?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var item = await Project(context.Tenants.Where(t => t.Slug == slug))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return item?.ToResponse();
    }

    public async Task<TenantResponse?> GetByCustomDomainAsync(string domain, CancellationToken cancellationToken = default)
    {
        var item = await Project(context.Tenants.Where(t => t.CustomDomain == domain))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return item?.ToResponse();
    }

    public async Task<IReadOnlyList<TenantResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var items = await Project(context.Tenants.OrderBy(t => t.Name))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return items.Select(t => t.ToResponse()).ToList().AsReadOnly();
    }

    public async Task<IReadOnlyList<TenantResponse>> GetByStatusAsync(
        TenantStatus status,
        CancellationToken cancellationToken = default)
    {
        var items = await Project(context.Tenants.Where(t => t.Status == status).OrderBy(t => t.Name))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return items.Select(t => t.ToResponse()).ToList().AsReadOnly();
    }

    public async Task<(IReadOnlyList<TenantResponse> Items, bool HasNextPage)> GetPagedAsync(
        int pageSize,
        string? afterName,
        Guid? afterId,
        CancellationToken cancellationToken = default)
    {
        var query = context.Tenants.AsQueryable();

        if (afterName is not null && afterId is not null)
            query = query.Where(t =>
                string.Compare(t.Name, afterName, StringComparison.Ordinal) > 0
                || (t.Name == afterName && t.Id.CompareTo(afterId.Value) > 0));

        var items = await Project(query
            .OrderBy(t => t.Name)
            .ThenBy(t => t.Id)
            .Take(pageSize + 1))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var hasNextPage = items.Count > pageSize;
        var page = items.Take(pageSize).Select(t => t.ToResponse()).ToList().AsReadOnly();
        return (page, hasNextPage);
    }

    private static IQueryable<TenantReadProjection> Project(IQueryable<global::Homeless.Domain.Entities.Tenant> query)
        => query.Select(t => new TenantReadProjection(
            t.Id,
            t.Name,
            t.Type,
            t.Slug,
            t.CustomDomain,
            t.FrontendOrigin,
            t.Status,
            t.PlanId,
            t.OwnerUserId,
            t.Branding,
            t.Entitlements,
            t.StripeCustomerId,
            t.StripeSubscriptionId,
            t.PaymentStatus,
            t.LastPaymentDate,
            t.NextBillingDate,
            t.ContactEmail,
            t.ContactPhone,
            t.CreatedAt,
            t.UpdatedAt));
}

internal sealed record TenantReadProjection(
    Guid Id,
    string Name,
    TenantType Type,
    string Slug,
    string? CustomDomain,
    string? FrontendOrigin,
    TenantStatus Status,
    Guid? PlanId,
    Guid? OwnerUserId,
    BrandingConfig Branding,
    Dictionary<string, object> Entitlements,
    string? StripeCustomerId,
    string? StripeSubscriptionId,
    string PaymentStatus,
    DateTime? LastPaymentDate,
    DateTime? NextBillingDate,
    string ContactEmail,
    string? ContactPhone,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    public TenantResponse ToResponse()
    {
        var portalTheme = ResolvePortalTheme();
        var portalLayoutMode = FrontendOrigin is { Length: > 0 }
            ? "Custom"
            : portalTheme switch
            {
                "minimal" => "Minimal",
                "premium" => "Premium",
                _ => "Default"
            };

        return new TenantResponse(
            Id,
            Name,
            Type,
            Slug,
            CustomDomain,
            FrontendOrigin,
            Status,
            PlanId,
            OwnerUserId,
            portalLayoutMode,
            portalTheme,
            ContactEmail,
            ContactPhone,
            new BrandingConfigResponse(
                Branding.LogoUrl,
                Branding.PrimaryColor,
                Branding.SecondaryColor,
                Branding.AgencyDisplayName,
                Branding.Tagline,
                Branding.FaviconUrl,
                Branding.PortalContent is null ? null : new PortalBrandingContentResponse(
                    Branding.PortalContent.HeroImageUrl,
                    Branding.PortalContent.HeroHeadline,
                    Branding.PortalContent.HeroSubhead,
                    Branding.PortalContent.HeroCtaLabel,
                    Branding.PortalContent.HeroCtaUrl,
                    Branding.PortalContent.ListingIntro,
                    Branding.PortalContent.SecondaryHeroImageUrl,
                    Branding.PortalContent.Stat1Label,
                    Branding.PortalContent.Stat1Value,
                    Branding.PortalContent.Stat2Label,
                    Branding.PortalContent.Stat2Value,
                    Branding.PortalContent.FeatureCard1Title,
                    Branding.PortalContent.FeatureCard1Description,
                    Branding.PortalContent.FeatureCard1Icon,
                    Branding.PortalContent.FeatureCard2Title,
                    Branding.PortalContent.FeatureCard2Description,
                    Branding.PortalContent.FeatureCard2Icon,
                    Branding.PortalContent.FeatureCard3Title,
                    Branding.PortalContent.FeatureCard3Description,
                    Branding.PortalContent.FeatureCard3Icon,
                    Branding.PortalContent.WhatsappNumber,
                    Branding.PortalContent.HeroSecondaryCta)),
            Entitlements,
            StripeCustomerId,
            StripeSubscriptionId,
            PaymentStatus,
            LastPaymentDate,
            NextBillingDate,
            CreatedAt,
            UpdatedAt);
    }

    private string ResolvePortalTheme()
    {
        if (!Entitlements.TryGetValue("portal_theme", out var raw) || raw is null)
            return "default";

        var s = raw switch
        {
            string str => str,
            JsonElement je when je.ValueKind == JsonValueKind.String => je.GetString() ?? "",
            _ => raw.ToString() ?? ""
        };

        return s.Trim().ToLowerInvariant() switch
        {
            "minimal" => "minimal",
            "premium" => "premium",
            _ => "default"
        };
    }
}
