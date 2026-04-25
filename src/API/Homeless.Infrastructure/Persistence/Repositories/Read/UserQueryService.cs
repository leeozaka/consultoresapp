using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Homeless.Application.DTOs;
using Homeless.Application.Interfaces;
using Homeless.Infrastructure.Identity;
using Homeless.Infrastructure.Persistence.Context;

namespace Homeless.Infrastructure.Persistence.Repositories.Read;

public sealed class UserQueryService(
    UserManager<ApplicationUser> userManager,
    ApplicationDbContext dbContext) : IUserQueryService
{
    public async Task<IReadOnlyList<UserResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var users = await dbContext.Users
            .AsNoTracking()
            .OrderByDescending(u => u.CreatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return await ProjectUsersAsync(users, cancellationToken);
    }

    public async Task<IReadOnlyList<UserResponse>> GetOrphanedAsync(CancellationToken cancellationToken = default)
    {
        var users = await dbContext.Users
            .AsNoTracking()
            .Where(u => u.TenantId == null)
            .OrderByDescending(u => u.CreatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return await ProjectUsersAsync(users, cancellationToken);
    }

    public async Task<UserResponse?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            .ConfigureAwait(false);

        if (user is null) return null;

        var list = await ProjectUsersAsync([user], cancellationToken);
        return list[0];
    }

    public async Task<IReadOnlyList<UserResponse>> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var users = await dbContext.Users
            .AsNoTracking()
            .Where(u => u.TenantId == tenantId)
            .OrderByDescending(u => u.CreatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return await ProjectUsersAsync(users, cancellationToken);
    }

    private async Task<IReadOnlyList<UserResponse>> ProjectUsersAsync(
        List<ApplicationUser> users,
        CancellationToken cancellationToken)
    {
        var tenantIds = users
            .Where(u => u.TenantId.HasValue)
            .Select(u => u.TenantId!.Value)
            .Distinct()
            .ToList();

        var tenantNames = tenantIds.Count > 0
            ? await dbContext.Tenants
                .AsNoTracking()
                .Where(t => tenantIds.Contains(t.Id))
                .ToDictionaryAsync(t => t.Id, t => t.Name, cancellationToken)
                .ConfigureAwait(false)
            : [];

        var result = new List<UserResponse>(users.Count);

        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user).ConfigureAwait(false);

            tenantNames.TryGetValue(user.TenantId ?? Guid.Empty, out var tenantName);

            result.Add(new UserResponse(
                user.Id,
                user.Email!,
                user.FirstName,
                user.LastName,
                user.TenantId,
                tenantName,
                roles.ToList().AsReadOnly(),
                user.IsActive,
                user.CreatedAtUtc));
        }

        return result.AsReadOnly();
    }
}
