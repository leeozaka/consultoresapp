using Ardalis.Result;
using MediatR;
using Homeless.Application.Interfaces;
using Homeless.Domain.Enums;

namespace Homeless.Application.UseCases.Onboarding;

public sealed class GetSignupResumeHandler(
    ITenantQueryService tenantQueryService,
    IIdentityService identityService)
    : IRequestHandler<GetSignupResumeQuery, Result<SignupResumeResponse>>
{
    public async Task<Result<SignupResumeResponse>> Handle(
        GetSignupResumeQuery request,
        CancellationToken cancellationToken)
    {
        var tenant = await tenantQueryService
            .GetByIdAsync(request.TenantId, cancellationToken)
            .ConfigureAwait(false);

        if (tenant is null || tenant.Status != TenantStatus.Pending || tenant.PaymentStatus != "none")
            return Result.NotFound($"Tenant '{request.TenantId}' not found or not resumable.");

        if (tenant.OwnerUserId is null || tenant.PlanId is null)
            return Result.NotFound($"Tenant '{request.TenantId}' is incomplete and not resumable.");

        var userInfo = await identityService
            .GetUserBasicInfoAsync(tenant.OwnerUserId.Value, cancellationToken)
            .ConfigureAwait(false);

        if (userInfo is null)
            return Result.NotFound($"Owner user for tenant '{request.TenantId}' not found.");

        var onboardingState = DeriveOnboardingState(tenant.Status, tenant.PaymentStatus);

        return Result.Success(new SignupResumeResponse(
            TenantId: tenant.Id,
            AgencyName: tenant.Name,
            Slug: tenant.Slug,
            ContactEmail: tenant.ContactEmail ?? string.Empty,
            ContactPhone: tenant.ContactPhone,
            PlanId: tenant.PlanId.Value,
            OwnerEmail: userInfo.Value.Email,
            OwnerFirstName: userInfo.Value.FirstName,
            OwnerLastName: userInfo.Value.LastName,
            OnboardingState: onboardingState));
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
