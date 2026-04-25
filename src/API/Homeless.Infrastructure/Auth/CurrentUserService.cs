using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Homeless.Application.Authorization;
using Homeless.Application.Interfaces;
using Authorization = Homeless.Application.Authorization;

namespace Homeless.Infrastructure.Auth;

/// <summary>
/// Extracts the current user's identity from the OpenIddict JWT claims stored
/// in the HttpOnly cookie. Used by MediatR pipeline behaviors (Authorization, Audit).
/// </summary>
public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated =>
        User?.Identity?.IsAuthenticated == true;

    public Guid UserId
    {
        get
        {
            var id = User?.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User?.FindFirstValue("sub");
            return id is not null && Guid.TryParse(id, out var guid) ? guid : Guid.Empty;
        }
    }

    public Guid TenantId
    {
        get
        {
            var id = User?.FindFirstValue("tenant_id");
            return id is not null && Guid.TryParse(id, out var guid) ? guid : Guid.Empty;
        }
    }

    public string Email =>
        User?.FindFirstValue(ClaimTypes.Email)
        ?? User?.FindFirstValue("email")
        ?? string.Empty;

    public string FullName =>
        User?.FindFirstValue("name")
        ?? User?.FindFirstValue(ClaimTypes.Name)
        ?? string.Empty;

    public IReadOnlyList<string> Roles =>
        User?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList() ?? [];

    public bool IsInRole(string role) =>
        User?.IsInRole(role) == true;

    public bool IsSuperAdmin =>
        IsInRole(Authorization.Roles.SuperAdmin);
}
