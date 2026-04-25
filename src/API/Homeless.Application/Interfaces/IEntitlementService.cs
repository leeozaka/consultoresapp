namespace Homeless.Application.Interfaces;

/// <summary>
/// Checks whether the current tenant has access to a specific feature.
/// Backed by the Tenant.Entitlements JSONB column with an in-memory cache layer.
/// </summary>
public interface IEntitlementService
{
    /// <summary>Returns true if the tenant's entitlements enable the given feature key.</summary>
    Task<bool> HasFeatureAsync(Guid tenantId, string featureKey, CancellationToken cancellationToken = default);

    /// <summary>Returns the raw entitlements dictionary.</summary>
    Task<Dictionary<string, object>> GetEntitlementsAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>Returns a typed entitlement value (e.g. GetEntitlementAsync&lt;int&gt;("max_properties")).</summary>
    Task<T?> GetEntitlementAsync<T>(Guid tenantId, string featureKey, CancellationToken cancellationToken = default);
}
