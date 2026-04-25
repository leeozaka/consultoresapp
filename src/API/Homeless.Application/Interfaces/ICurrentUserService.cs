namespace Homeless.Application.Interfaces;

/// <summary>
/// Scoped service exposing the authenticated user's identity within the current request.
/// Claims are sourced from the OpenIddict JWT, which is issued with tenant_id and role claims.
/// </summary>
public interface ICurrentUserService
{
    Guid UserId { get; }
    Guid TenantId { get; }
    string Email { get; }
    string FullName { get; }
    IReadOnlyList<string> Roles { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string role);
    bool IsSuperAdmin { get; }
}
