using Ardalis.Result;
using MediatR;
using Homeless.Application.Interfaces;
using Homeless.Domain.Enums;

namespace Homeless.Application.UseCases.Onboarding;

public sealed class GetOnboardingStatusHandler(ITenantQueryService tenantQueryService)
    : IRequestHandler<GetOnboardingStatusQuery, Result<OnboardingStatusResponse>>
{
    public async Task<Result<OnboardingStatusResponse>> Handle(
        GetOnboardingStatusQuery request,
        CancellationToken cancellationToken)
    {
        var tenant = await tenantQueryService
            .GetByIdAsync(request.TenantId, cancellationToken)
            .ConfigureAwait(false);

        if (tenant is null)
            return Result.NotFound($"Tenant '{request.TenantId}' not found.");

        var onboardingState = DeriveOnboardingState(tenant.Status, tenant.PaymentStatus);

        return Result.Success(new OnboardingStatusResponse(
            TenantId: tenant.Id,
            TenantStatus: tenant.Status.ToString().ToLowerInvariant(),
            PaymentStatus: tenant.PaymentStatus,
            OnboardingState: onboardingState,
            Slug: tenant.Slug));
    }

    private static string DeriveOnboardingState(TenantStatus status, string paymentStatus) =>
        status switch
        {
            TenantStatus.Active => "active",
            TenantStatus.Pending when paymentStatus is "paid" => "awaiting_approval",
            TenantStatus.Pending when paymentStatus is "none" or "" => "pending_payment",
            TenantStatus.Pending => "payment_confirmed",
            TenantStatus.Suspended => "suspended",
            TenantStatus.Archived => "archived",
            _ => "pending_payment"
        };
}
