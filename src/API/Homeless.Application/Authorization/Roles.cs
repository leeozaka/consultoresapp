namespace Homeless.Application.Authorization;

/// <summary>
/// Strongly-typed role constants matching the seeded ASP.NET Identity roles.
/// Used on RequireRole attributes, Authorize policies, and Angular route guards.
/// </summary>
public static class Roles
{
    /// <summary>Platform owner — can manage all tenants, plans, and billing.</summary>
    public const string SuperAdmin = "SuperAdmin";

    /// <summary>Agency owner — manages agents, properties, and their tenant configuration.</summary>
    public const string TenantAdmin = "TenantAdmin";

    /// <summary>Agency employee — creates and manages property listings.</summary>
    public const string Agent = "Agent";

    public static readonly IReadOnlyList<string> All = [SuperAdmin, TenantAdmin, Agent];
}
