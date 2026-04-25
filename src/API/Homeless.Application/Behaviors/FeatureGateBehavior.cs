using Ardalis.Result;
using MediatR;
using Homeless.Application.Authorization;
using Homeless.Application.Interfaces;

namespace Homeless.Application.Behaviors;

/// <summary>
/// MediatR pipeline behavior that enforces tenant feature entitlements.
/// Checks [RequireFeature("key")] attribute on Commands. Returns Result.Forbidden()
/// if the current tenant does not have the required feature enabled.
/// Runs after AuthorizationBehavior.
/// </summary>
public sealed class FeatureGateBehavior<TRequest, TResponse>(
    ICurrentUserService currentUser,
    IEntitlementService entitlementService)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requireFeature = (RequireFeatureAttribute?)Attribute.GetCustomAttribute(
            typeof(TRequest), typeof(RequireFeatureAttribute));

        if (requireFeature is null)
            return await next().ConfigureAwait(false);

        // SuperAdmins bypass feature gates
        if (currentUser.IsSuperAdmin)
            return await next().ConfigureAwait(false);

        var hasFeature = await entitlementService
            .HasFeatureAsync(currentUser.TenantId, requireFeature.FeatureKey, cancellationToken)
            .ConfigureAwait(false);

        if (!hasFeature)
            return CreateForbiddenResult<TResponse>(requireFeature.FeatureKey);

        return await next().ConfigureAwait(false);
    }

    private static TResponse CreateForbiddenResult<T>(string featureKey)
    {
        var resultType = typeof(T);

        if (resultType == typeof(Result))
            return (TResponse)(object)Result.Forbidden($"Feature '{featureKey}' is not enabled for your plan.");

        if (resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            var inner = resultType.GetGenericArguments()[0];
            var method = typeof(Result<>)
                .MakeGenericType(inner)
                .GetMethod(nameof(Result.Forbidden), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static, null, [typeof(string[])], null);

            if (method is not null)
                return (TResponse)method.Invoke(null, [new[] { $"Feature '{featureKey}' is not enabled for your plan." }])!;
        }

        throw new InvalidOperationException($"Feature '{featureKey}' is not available on your current plan.");
    }
}
