namespace Homeless.Application.Options;

public sealed class CachingOptions
{
    public const string SectionName = "Caching";

    /// <summary>Cache TTL for individual property lookups (seconds).</summary>
    public int PropertyTtlSeconds { get; init; } = 300;

    /// <summary>Cache TTL for idempotency keys (seconds).</summary>
    public int IdempotencyTtlSeconds { get; init; } = 300;

    /// <summary>Cache TTL for tenant entitlement lookups (seconds).</summary>
    public int EntitlementTtlSeconds { get; init; } = 600;

    /// <summary>Cache TTL for resolved tenant objects (seconds).</summary>
    public int TenantTtlSeconds { get; init; } = 300;

    /// <summary>Cache TTL for location suggestion lookups (seconds).</summary>
    public int LocationSuggestionsTtlSeconds { get; init; } = 300;
}
