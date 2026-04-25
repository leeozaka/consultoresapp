namespace Homeless.Application.Interfaces;

/// <summary>
/// Scoped service holding the resolved tenant for the current HTTP request.
/// Populated by TenantResolutionMiddleware and injected into DbContexts to enforce
/// row-level isolation via Global Query Filters and PostgreSQL RLS.
/// </summary>
public interface ITenantContext
{
    Guid TenantId { get; }
    string TenantSlug { get; }

    /// <summary>False if middleware could not resolve a tenant (e.g. SuperAdmin direct-access routes).</summary>
    bool IsResolved { get; }

    void SetTenant(Guid tenantId, string slug);
}
