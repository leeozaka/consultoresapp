using Microsoft.AspNetCore.Identity;
using Homeless.Application.Interfaces;
using Homeless.Infrastructure.Identity;

namespace Homeless.Infrastructure.Persistence.Repositories.Write;

public sealed class UserCommandService(
    UserManager<ApplicationUser> userManager) : IUserCommandService
{
    public async Task<bool> SetActiveAsync(Guid userId, bool isActive, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString()).ConfigureAwait(false);
        if (user is null) return false;

        user.IsActive = isActive;

        if (!isActive)
        {
            // Lock the user out indefinitely
            await userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue).ConfigureAwait(false);
        }
        else
        {
            // Remove lockout
            await userManager.SetLockoutEndDateAsync(user, null).ConfigureAwait(false);
            await userManager.ResetAccessFailedCountAsync(user).ConfigureAwait(false);
        }

        await userManager.UpdateAsync(user).ConfigureAwait(false);
        return true;
    }
}
