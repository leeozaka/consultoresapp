using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Homeless.Application.Interfaces;
using Homeless.Infrastructure.Persistence.Context;

namespace Homeless.Infrastructure.Identity;

public sealed class IdentityService(
    UserManager<ApplicationUser> userManager,
    ApplicationDbContext dbContext) : IIdentityService
{
    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default) =>
        await userManager.FindByEmailAsync(email).ConfigureAwait(false) is not null;

    public async Task<(bool Success, Guid UserId, IReadOnlyList<string> Errors)> CreateUserAsync(
        string email,
        string password,
        string? firstName,
        string? lastName,
        CancellationToken cancellationToken = default)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            EmailConfirmed = false,
            FirstName = firstName ?? string.Empty,
            LastName = lastName ?? string.Empty,
            IsActive = false,   // activated by SuperAdmin on tenant assignment or after payment
            CreatedAtUtc = DateTime.UtcNow,
        };

        var result = await userManager.CreateAsync(user, password).ConfigureAwait(false);

        return result.Succeeded
            ? (true, user.Id, Array.Empty<string>())
            : (false, Guid.Empty, result.Errors.Select(e => e.Description).ToList().AsReadOnly());
    }

    public async Task<bool> UserExistsAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await userManager.FindByIdAsync(userId.ToString()).ConfigureAwait(false) is not null;

    public async Task<bool> TenantExistsAsync(Guid tenantId, CancellationToken cancellationToken = default) =>
        await dbContext.Tenants
            .AsNoTracking()
            .AnyAsync(t => t.Id == tenantId, cancellationToken)
            .ConfigureAwait(false);

    public async Task<(bool Success, IReadOnlyList<string> Errors)> AssignTenantAndRoleAsync(
        Guid userId,
        Guid tenantId,
        string role,
        bool activate = true,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString()).ConfigureAwait(false);
        if (user is null)
            return (false, ["User not found."]);

        var currentRoles = await userManager.GetRolesAsync(user).ConfigureAwait(false);
        if (currentRoles.Count > 0)
            await userManager.RemoveFromRolesAsync(user, currentRoles).ConfigureAwait(false);

        user.TenantId = tenantId;
        user.IsActive = activate;

        var updateResult = await userManager.UpdateAsync(user).ConfigureAwait(false);
        if (!updateResult.Succeeded)
            return (false, updateResult.Errors.Select(e => e.Description).ToList().AsReadOnly());

        var roleResult = await userManager.AddToRoleAsync(user, role).ConfigureAwait(false);
        return roleResult.Succeeded
            ? (true, Array.Empty<string>())
            : (false, roleResult.Errors.Select(e => e.Description).ToList().AsReadOnly());
    }

    public async Task ActivateUsersByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var users = await dbContext.Users
            .Where(u => u.TenantId == tenantId && !u.IsActive)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var user in users)
        {
            user.IsActive = true;
            await userManager.UpdateAsync(user).ConfigureAwait(false);
        }
    }

    public async Task<string?> GetUserEmailAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString()).ConfigureAwait(false);
        return user?.Email;
    }

    public async Task<(string Email, string FirstName, string LastName)?> GetUserBasicInfoAsync(
        Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString()).ConfigureAwait(false);
        if (user is null) return null;
        return (user.Email ?? string.Empty, user.FirstName, user.LastName);
    }

    public async Task<(bool Success, IReadOnlyList<string> Errors)> UpdateUserAsync(
        Guid userId,
        string? firstName,
        string? lastName,
        string? email,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString()).ConfigureAwait(false);
        if (user is null)
            return (false, ["User not found."]);

        if (firstName is not null) user.FirstName = firstName;
        if (lastName is not null) user.LastName = lastName;

        if (email is not null && !string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
        {
            user.Email = email;
            user.UserName = email;
            user.NormalizedEmail = email.ToUpperInvariant();
            user.NormalizedUserName = email.ToUpperInvariant();
        }

        var result = await userManager.UpdateAsync(user).ConfigureAwait(false);
        return result.Succeeded
            ? (true, Array.Empty<string>())
            : (false, result.Errors.Select(e => e.Description).ToList().AsReadOnly());
    }

    public async Task<bool> DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString()).ConfigureAwait(false);
        if (user is null) return true; // already gone

        var result = await userManager.DeleteAsync(user).ConfigureAwait(false);
        return result.Succeeded;
    }
}
