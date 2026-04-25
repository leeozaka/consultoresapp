using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Homeless.Application.Interfaces;
using Homeless.Application.Options;
using Homeless.Infrastructure.Persistence.Context;

namespace Homeless.Infrastructure.Entitlements;

/// <summary>
/// Resolves tenant feature entitlements from the DB with a short-lived cache.
/// Cache invalidation is handled when ActivateTenantCommand or UpdateEntitlementsCommand invalidates it.
/// </summary>
public sealed class EntitlementService(
    IServiceProvider serviceProvider,
    ICacheService cacheService,
    IOptions<CachingOptions> cachingOptions)
    : IEntitlementService
{
    private readonly CachingOptions _caching = cachingOptions.Value;

    public async Task<bool> HasFeatureAsync(
        Guid tenantId,
        string featureKey,
        CancellationToken cancellationToken = default)
    {
        var entitlements = await GetEntitlementsAsync(tenantId, cancellationToken).ConfigureAwait(false);

        if (!entitlements.TryGetValue(featureKey, out var value))
            return false;

        return value switch
        {
            bool b => b,
            string s => bool.TryParse(s, out var parsed) && parsed,
            JsonElement je when je.ValueKind == JsonValueKind.True => true,
            JsonElement je when je.ValueKind == JsonValueKind.False => false,
            JsonElement je when je.ValueKind == JsonValueKind.String
                => bool.TryParse(je.GetString(), out var p) && p,
            _ => false
        };
    }

    public async Task<Dictionary<string, object>> GetEntitlementsAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheKeys.TenantEntitlements(tenantId);

        var cached = await cacheService
            .GetAsync<Dictionary<string, object>>(cacheKey, cancellationToken)
            .ConfigureAwait(false);

        if (cached is not null)
            return cached;

        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var entitlements = await db.Tenants
            .Where(t => t.Id == tenantId)
            .Select(t => t.Entitlements)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        var result = entitlements ?? [];
        await cacheService.SetAsync(cacheKey, result, TimeSpan.FromSeconds(_caching.EntitlementTtlSeconds), cancellationToken).ConfigureAwait(false);

        return result;
    }

    public async Task<T?> GetEntitlementAsync<T>(
        Guid tenantId,
        string featureKey,
        CancellationToken cancellationToken = default)
    {
        var entitlements = await GetEntitlementsAsync(tenantId, cancellationToken).ConfigureAwait(false);

        if (!entitlements.TryGetValue(featureKey, out var value))
            return default;

        if (value is JsonElement je)
        {
            try { return je.Deserialize<T>(); }
            catch { return default; }
        }

        try { return (T)Convert.ChangeType(value, typeof(T)); }
        catch { return default; }
    }
}
