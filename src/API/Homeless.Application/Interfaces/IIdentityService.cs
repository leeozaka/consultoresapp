namespace Homeless.Application.Interfaces;

/// <summary>
/// Abstracts identity write operations (user creation, role assignment, tenant binding)
/// so Application-layer handlers stay independent of ASP.NET Core Identity types.
/// </summary>
public interface IIdentityService
{
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new orphan user (no tenant assigned yet).
    /// </summary>
    /// <returns>
    /// On success: <c>(true, newUserId, [])</c>.
    /// On failure: <c>(false, Guid.Empty, errors)</c>.
    /// </returns>
    Task<(bool Success, Guid UserId, IReadOnlyList<string> Errors)> CreateUserAsync(
        string email,
        string password,
        string? firstName,
        string? lastName,
        CancellationToken cancellationToken = default);

    Task<bool> UserExistsAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<bool> TenantExistsAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Binds a user to a tenant and replaces any existing roles with <paramref name="role"/>.
    /// When <paramref name="activate"/> is <c>false</c> the user stays inactive (IsActive = false)
    /// until explicitly activated, e.g. after payment or SuperAdmin approval.
    /// </summary>
    Task<(bool Success, IReadOnlyList<string> Errors)> AssignTenantAndRoleAsync(
        Guid userId,
        Guid tenantId,
        string role,
        bool activate = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Activates all users bound to the given tenant (sets IsActive = true).
    /// Called after a Pending tenant is approved or auto-activated on checkout completion.
    /// </summary>
    Task ActivateUsersByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>Returns the email address for the given user, or null if not found.</summary>
    Task<string?> GetUserEmailAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Returns basic identity info for the given user, or null if not found.</summary>
    Task<(string Email, string FirstName, string LastName)?> GetUserBasicInfoAsync(
        Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Updates user profile fields (name and/or email).</summary>
    Task<(bool Success, IReadOnlyList<string> Errors)> UpdateUserAsync(
        Guid userId,
        string? firstName,
        string? lastName,
        string? email,
        CancellationToken cancellationToken = default);

    /// <summary>Permanently deletes the user account. Used for compensating signup rollback.</summary>
    Task<bool> DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
