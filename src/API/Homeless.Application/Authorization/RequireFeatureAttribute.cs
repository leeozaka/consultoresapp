namespace Homeless.Application.Authorization;

/// <summary>
/// Marks a MediatR Command as requiring a specific tenant entitlement feature flag.
/// Enforced by FeatureGateBehavior. Returns Result.Forbidden() if the tenant lacks the feature.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class RequireFeatureAttribute(string featureKey) : Attribute
{
    public string FeatureKey { get; } = featureKey;
}
