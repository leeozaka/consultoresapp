using Microsoft.AspNetCore.Identity;

namespace Homeless.Infrastructure.Identity;

/// <summary>
/// Extends IdentityUser with Consultores-specific properties.
/// Scoped to a specific tenant (agency), except for SuperAdmin users (TenantId = null).
/// </summary>
public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Null for SuperAdmins, set for TenantAdmin and Agent users.
    /// Cryptographically bound to the JWT tenant_id claim.
    /// </summary>
    public Guid? TenantId { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public string FullName => $"{FirstName} {LastName}".Trim();
}
