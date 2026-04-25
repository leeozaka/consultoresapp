namespace Homeless.Domain.Enums;

public enum TenantStatus
{
    /// <summary>Tenant registered but not yet configured by a SuperAdmin.</summary>
    Pending,

    /// <summary>Fully configured, operational tenant.</summary>
    Active,

    /// <summary>Temporarily suspended (billing overdue, violation, etc.).</summary>
    Suspended,

    /// <summary>Permanently closed — data retained for legal purposes.</summary>
    Archived
}
