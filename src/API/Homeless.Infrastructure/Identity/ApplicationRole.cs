using Microsoft.AspNetCore.Identity;

namespace Homeless.Infrastructure.Identity;

/// <summary>
/// Application role. Seeded on startup: SuperAdmin, TenantAdmin, Agent.
/// </summary>
public sealed class ApplicationRole : IdentityRole<Guid>
{
    public ApplicationRole() { }
    public ApplicationRole(string roleName) : base(roleName) { }
}
