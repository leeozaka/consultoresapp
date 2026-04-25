namespace Homeless.Application.Authorization;

/// <summary>
/// Marks a MediatR Command or Query as requiring specific roles.
/// Enforced by AuthorizationBehavior before the handler executes.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class RequireRoleAttribute(params string[] roles) : Attribute
{
    public IReadOnlyList<string> Roles { get; } = roles;
}
