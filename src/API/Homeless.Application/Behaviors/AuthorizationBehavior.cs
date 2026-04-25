using Ardalis.Result;
using MediatR;
using Homeless.Application.Authorization;
using Homeless.Application.Interfaces;

namespace Homeless.Application.Behaviors;

/// <summary>
/// MediatR pipeline behavior that enforces role-based access control.
/// Checks [RequireRole(...)] attribute on the incoming Command or Query
/// before executing the handler. Runs after LoggingBehavior.
/// </summary>
public sealed class AuthorizationBehavior<TRequest, TResponse>(
    ICurrentUserService currentUser)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requireRole = (RequireRoleAttribute?)Attribute.GetCustomAttribute(
            typeof(TRequest), typeof(RequireRoleAttribute));

        if (requireRole is null)
            return await next().ConfigureAwait(false);

        if (!currentUser.IsAuthenticated)
            return CreateUnauthorizedResult<TResponse>();

        // SuperAdmins bypass role-based restrictions (same as FeatureGateBehavior)
        if (currentUser.IsSuperAdmin)
            return await next().ConfigureAwait(false);

        var hasRole = requireRole.Roles.Any(currentUser.IsInRole);
        if (!hasRole)
            return CreateForbiddenResult<TResponse>();

        return await next().ConfigureAwait(false);
    }

    private static TResponse CreateUnauthorizedResult<T>()
    {
        var resultType = typeof(T);

        if (resultType == typeof(Result))
            return (TResponse)(object)Result.Unauthorized();

        if (resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            var inner = resultType.GetGenericArguments()[0];
            var method = typeof(Result<>)
                .MakeGenericType(inner)
                .GetMethod(nameof(Result.Unauthorized), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

            if (method is not null)
                return (TResponse)method.Invoke(null, null)!;
        }

        throw new UnauthorizedAccessException("Unauthorized.");
    }

    private static TResponse CreateForbiddenResult<T>()
    {
        var resultType = typeof(T);

        if (resultType == typeof(Result))
            return (TResponse)(object)Result.Forbidden();

        if (resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            var inner = resultType.GetGenericArguments()[0];
            var method = typeof(Result<>)
                .MakeGenericType(inner)
                .GetMethod(nameof(Result.Forbidden), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

            if (method is not null)
                return (TResponse)method.Invoke(null, null)!;
        }

        throw new UnauthorizedAccessException("Forbidden.");
    }
}
