using Homeless.Application.Interfaces;

namespace Homeless.Infrastructure.Auth;

/// <summary>
/// Scoped implementation of ITenantContext.
/// Populated by TenantResolutionMiddleware for each HTTP request.
/// </summary>
public sealed class TenantContext : ITenantContext
{
    public Guid TenantId { get; private set; }
    public string TenantSlug { get; private set; } = string.Empty;
    public bool IsResolved { get; private set; }

    public void SetTenant(Guid tenantId, string slug)
    {
        TenantId = tenantId;
        TenantSlug = slug;
        IsResolved = true;
    }
}
