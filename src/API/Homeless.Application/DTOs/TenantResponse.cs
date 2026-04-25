using System.Text.Json.Serialization;
using Homeless.Domain.Enums;
using Homeless.Domain.ValueObjects;

namespace Homeless.Application.DTOs;

public sealed record TenantResponse(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("type")] TenantType Type,
    [property: JsonPropertyName("slug")] string Slug,
    [property: JsonPropertyName("custom_domain")] string? CustomDomain,
    [property: JsonPropertyName("frontend_origin")] string? FrontendOrigin,
    [property: JsonPropertyName("status")] TenantStatus Status,
    [property: JsonPropertyName("plan_id")] Guid? PlanId,
    [property: JsonPropertyName("owner_user_id")] Guid? OwnerUserId,
    [property: JsonPropertyName("portal_layout_mode")] string PortalLayoutMode,
    [property: JsonPropertyName("portal_theme")] string PortalTheme,
    [property: JsonPropertyName("contact_email")] string ContactEmail,
    [property: JsonPropertyName("contact_phone")] string? ContactPhone,
    [property: JsonPropertyName("branding")] BrandingConfigResponse Branding,
    [property: JsonPropertyName("entitlements")] Dictionary<string, object> Entitlements,
    [property: JsonPropertyName("stripe_customer_id")] string? StripeCustomerId,
    [property: JsonPropertyName("stripe_subscription_id")] string? StripeSubscriptionId,
    [property: JsonPropertyName("payment_status")] string PaymentStatus,
    [property: JsonPropertyName("last_payment_date")] DateTime? LastPaymentDate,
    [property: JsonPropertyName("next_billing_date")] DateTime? NextBillingDate,
    [property: JsonPropertyName("created_at")] DateTime CreatedAt,
    [property: JsonPropertyName("updated_at")] DateTime UpdatedAt
);

public sealed record PortalBrandingContentResponse(
    [property: JsonPropertyName("hero_image_url")] string? HeroImageUrl,
    [property: JsonPropertyName("hero_headline")] string? HeroHeadline,
    [property: JsonPropertyName("hero_subhead")] string? HeroSubhead,
    [property: JsonPropertyName("hero_cta_label")] string? HeroCtaLabel,
    [property: JsonPropertyName("hero_cta_url")] string? HeroCtaUrl,
    [property: JsonPropertyName("listing_intro")] string? ListingIntro,
    [property: JsonPropertyName("secondary_hero_image_url")] string? SecondaryHeroImageUrl,
    [property: JsonPropertyName("stat1_label")] string? Stat1Label = null,
    [property: JsonPropertyName("stat1_value")] string? Stat1Value = null,
    [property: JsonPropertyName("stat2_label")] string? Stat2Label = null,
    [property: JsonPropertyName("stat2_value")] string? Stat2Value = null,
    [property: JsonPropertyName("feature_card1_title")] string? FeatureCard1Title = null,
    [property: JsonPropertyName("feature_card1_description")] string? FeatureCard1Description = null,
    [property: JsonPropertyName("feature_card1_icon")] string? FeatureCard1Icon = null,
    [property: JsonPropertyName("feature_card2_title")] string? FeatureCard2Title = null,
    [property: JsonPropertyName("feature_card2_description")] string? FeatureCard2Description = null,
    [property: JsonPropertyName("feature_card2_icon")] string? FeatureCard2Icon = null,
    [property: JsonPropertyName("feature_card3_title")] string? FeatureCard3Title = null,
    [property: JsonPropertyName("feature_card3_description")] string? FeatureCard3Description = null,
    [property: JsonPropertyName("feature_card3_icon")] string? FeatureCard3Icon = null,
    [property: JsonPropertyName("whatsapp_number")] string? WhatsappNumber = null,
    [property: JsonPropertyName("hero_secondary_cta")] string? HeroSecondaryCta = null
);

public sealed record BrandingConfigResponse(
    [property: JsonPropertyName("logo_url")] string? LogoUrl,
    [property: JsonPropertyName("primary_color")] string PrimaryColor,
    [property: JsonPropertyName("secondary_color")] string SecondaryColor,
    [property: JsonPropertyName("agency_display_name")] string AgencyDisplayName,
    [property: JsonPropertyName("tagline")] string? Tagline,
    [property: JsonPropertyName("favicon_url")] string? FaviconUrl,
    [property: JsonPropertyName("portal_content")] PortalBrandingContentResponse? PortalContent
);

public sealed record CreateTenantRequest(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("slug")] string Slug,
    [property: JsonPropertyName("contact_email")] string ContactEmail,
    [property: JsonPropertyName("contact_phone")] string? ContactPhone = null,
    [property: JsonPropertyName("custom_domain")] string? CustomDomain = null,
    [property: JsonPropertyName("next_billing_date")] DateTime? NextBillingDate = null
);

public sealed record UpdateTenantRequest(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("slug")] string Slug,
    [property: JsonPropertyName("contact_email")] string ContactEmail,
    [property: JsonPropertyName("contact_phone")] string? ContactPhone = null,
    [property: JsonPropertyName("custom_domain")] string? CustomDomain = null,
    [property: JsonPropertyName("next_billing_date")] DateTime? NextBillingDate = null
);

public sealed record UpdateTenantSettingsRequest(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("contact_email")] string ContactEmail,
    [property: JsonPropertyName("contact_phone")] string? ContactPhone = null
);

public sealed record UpdateTenantBrandingRequest(
    [property: JsonPropertyName("logo_url")] string? LogoUrl,
    [property: JsonPropertyName("primary_color")] string PrimaryColor,
    [property: JsonPropertyName("secondary_color")] string SecondaryColor,
    [property: JsonPropertyName("agency_display_name")] string AgencyDisplayName,
    [property: JsonPropertyName("tagline")] string? Tagline = null,
    [property: JsonPropertyName("favicon_url")] string? FaviconUrl = null,
    [property: JsonPropertyName("portal_content")] PortalBrandingContentResponse? PortalContent = null
);

public sealed record SetTenantCustomDomainRequest(
    [property: JsonPropertyName("custom_domain")] string? CustomDomain
);

public sealed record SetTenantFrontendOriginRequest(
    [property: JsonPropertyName("frontend_origin")] string? FrontendOrigin
);

public sealed record PortalAssetUploadResponse(
    [property: JsonPropertyName("url")] string Url
);

public sealed record UpdateTenantEntitlementsRequest(
    [property: JsonPropertyName("entitlements")] Dictionary<string, object> Entitlements
);

public sealed record AssignTenantPlanRequest(
    [property: JsonPropertyName("plan_id")] Guid PlanId
);
