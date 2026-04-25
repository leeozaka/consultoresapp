namespace Homeless.Application.Authorization;

/// <summary>Known entitlement feature keys. Must match the keys in Tenant.Entitlements JSONB.</summary>
public static class FeatureKeys
{
    public const string MaxProperties    = "max_properties";
    public const string VideoUpload      = "video_upload";
    public const string AiDescriptions  = "ai_descriptions";
    public const string CustomDomain     = "custom_domain";
    public const string PremiumAnalytics = "premium_analytics";
}
