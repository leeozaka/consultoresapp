namespace Homeless.Application.Interfaces;

public static class CacheKeys
{
    private const string IdempotencyPrefix = "idempotency:";
    private const string WebhookEventPrefix = "webhook:event:";

    public static string Idempotency(string referenceId) => $"{IdempotencyPrefix}{referenceId}";

    public static string WebhookEvent(string eventId) => $"{WebhookEventPrefix}{eventId}";

    private const string TenantBySlugPrefix = "tenant:slug:";
    private const string TenantByDomainPrefix = "tenant:domain:";
    private const string TenantEntitlementsPrefix = "tenant:entitlements:";

    public static string TenantBySlug(string slug) => $"{TenantBySlugPrefix}{slug}";

    public static string TenantByDomain(string domain) => $"{TenantByDomainPrefix}{domain}";

    public static string TenantEntitlements(Guid tenantId) =>
        $"{TenantEntitlementsPrefix}{tenantId}";

    private const string PropertyPrefix = "property:";
    private const string PropertyCountPrefix = "property:count:";

    public static string Property(Guid propertyId) => $"{PropertyPrefix}{propertyId}";

    public static string PropertyCount(Guid tenantId) => $"{PropertyCountPrefix}{tenantId}";

    private const string LocationSuggestionsPrefix = "location:suggestions:";

    public static string LocationSuggestions(Guid tenantId, string? q) =>
        $"{LocationSuggestionsPrefix}{tenantId}:{q?.ToLowerInvariant().Trim() ?? ""}";
}
