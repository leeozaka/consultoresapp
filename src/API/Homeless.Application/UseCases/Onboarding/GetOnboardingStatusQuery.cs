using Ardalis.Result;
using MediatR;

namespace Homeless.Application.UseCases.Onboarding;

/// <summary>Query to poll current onboarding state for a tenant.</summary>
public sealed record GetOnboardingStatusQuery(Guid TenantId) : IRequest<Result<OnboardingStatusResponse>>;

public sealed record OnboardingStatusResponse(
    Guid TenantId,
    string TenantStatus,
    string PaymentStatus,

    /// <summary>
    /// Derived onboarding state: "pending_payment", "payment_confirmed", "provisioning",
    /// "awaiting_approval", "active".
    /// </summary>
    string OnboardingState,

    string? Slug
);
