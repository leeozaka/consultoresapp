using System.Text.Json.Serialization;

namespace Homeless.Application.DTOs;

public sealed record PlanStripeCatalogRequest(
    Guid PlanId,
    string Name,
    string Description,
    decimal PricePerMonth,
    string CurrencyCode,
    string? StripePriceId
);

public sealed record PlanStripeCatalogResult(
    string StripePriceId,
    string StripeProductId,
    bool CreatedProduct,
    bool CreatedPrice
);

public sealed record PlanResponse(
    [property: JsonPropertyName("id")]               Guid    Id,
    [property: JsonPropertyName("name")]             string  Name,
    [property: JsonPropertyName("description")]      string  Description,
    [property: JsonPropertyName("price_per_month")]  decimal PricePerMonth,
    [property: JsonPropertyName("currency_code")]    string  CurrencyCode,
    [property: JsonPropertyName("max_properties")]   int     MaxProperties,
    [property: JsonPropertyName("video_upload")]     bool    VideoUpload,
    [property: JsonPropertyName("ai_descriptions")]  bool    AiDescriptions,
    [property: JsonPropertyName("custom_domain")]    bool    CustomDomain,
    [property: JsonPropertyName("premium_analytics")] bool   PremiumAnalytics,
    [property: JsonPropertyName("portal_theme")]     string  PortalTheme,
    [property: JsonPropertyName("stripe_price_id")]  string? StripePriceId,
    [property: JsonPropertyName("is_active")]        bool    IsActive
);

public sealed record CreatePlanRequest(
    [property: JsonPropertyName("name")]             string  Name,
    [property: JsonPropertyName("description")]      string  Description,
    [property: JsonPropertyName("price_per_month")]  decimal PricePerMonth,
    [property: JsonPropertyName("max_properties")]   int     MaxProperties,
    [property: JsonPropertyName("video_upload")]     bool    VideoUpload        = false,
    [property: JsonPropertyName("ai_descriptions")]  bool    AiDescriptions     = false,
    [property: JsonPropertyName("custom_domain")]    bool    CustomDomain       = false,
    [property: JsonPropertyName("premium_analytics")] bool   PremiumAnalytics   = false,
    [property: JsonPropertyName("portal_theme")]     string  PortalTheme        = "default",
    [property: JsonPropertyName("stripe_price_id")]  string? StripePriceId      = null
);

public sealed record ChangePlanRequest(
    [property: System.Text.Json.Serialization.JsonPropertyName("plan_id")] Guid PlanId,
    [property: System.Text.Json.Serialization.JsonPropertyName("success_url")] string? SuccessUrl,
    [property: System.Text.Json.Serialization.JsonPropertyName("cancel_url")] string? CancelUrl);


public sealed record UpdatePlanRequest(
    [property: JsonPropertyName("name")]             string  Name,
    [property: JsonPropertyName("description")]      string  Description,
    [property: JsonPropertyName("price_per_month")]  decimal PricePerMonth,
    [property: JsonPropertyName("max_properties")]   int     MaxProperties,
    [property: JsonPropertyName("video_upload")]     bool    VideoUpload,
    [property: JsonPropertyName("ai_descriptions")]  bool    AiDescriptions,
    [property: JsonPropertyName("custom_domain")]    bool    CustomDomain,
    [property: JsonPropertyName("premium_analytics")] bool   PremiumAnalytics,
    [property: JsonPropertyName("portal_theme")]     string  PortalTheme,
    [property: JsonPropertyName("stripe_price_id")]  string? StripePriceId
);

public sealed record ChangePlanResponse(
    [property: JsonPropertyName("plan")]             PlanResponse Plan,
    [property: JsonPropertyName("requires_checkout")] bool RequiresCheckout,
    [property: JsonPropertyName("checkout_url")]     string? CheckoutUrl,
    [property: JsonPropertyName("updated_in_place")] bool UpdatedInPlace,
    [property: JsonPropertyName("message")]          string Message
);
