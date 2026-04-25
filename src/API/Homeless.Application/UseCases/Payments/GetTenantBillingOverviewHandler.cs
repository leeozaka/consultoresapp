using Ardalis.Result;
using MediatR;
using Homeless.Application.DTOs;
using Homeless.Application.Interfaces;

namespace Homeless.Application.UseCases.Payments;

public sealed class GetTenantBillingOverviewHandler(
    ITenantQueryService tenantQueryService,
    IPlanQueryService planQueryService,
    ITenantContext tenantContext,
    IPaymentGateway paymentGateway)
    : IRequestHandler<GetTenantBillingOverviewQuery, Result<TenantBillingOverviewResponse>>
{
    public async Task<Result<TenantBillingOverviewResponse>> Handle(
        GetTenantBillingOverviewQuery request,
        CancellationToken cancellationToken)
    {
        if (!tenantContext.IsResolved)
            return Result.NotFound("No tenant resolved for this request.");

        var tenant = await tenantQueryService
            .GetByIdAsync(tenantContext.TenantId, cancellationToken)
            .ConfigureAwait(false);

        if (tenant is null)
            return Result.NotFound("Tenant not found.");

        var planName = default(string);
        if (tenant.PlanId.HasValue)
        {
            var plan = await planQueryService
                .GetByIdAsync(tenant.PlanId.Value, cancellationToken)
                .ConfigureAwait(false);
            planName = plan?.Name;
        }

        var overview = await paymentGateway.GetBillingOverviewAsync(
            new BillingOverviewRequest(
                TenantId: tenant.Id,
                StripeCustomerId: tenant.StripeCustomerId,
                StripeSubscriptionId: tenant.StripeSubscriptionId,
                PaymentStatus: tenant.PaymentStatus,
                LastPaymentDate: tenant.LastPaymentDate,
                NextBillingDate: tenant.NextBillingDate,
                RecentTransactionsLimit: request.RecentTransactionsLimit),
            cancellationToken).ConfigureAwait(false);

        return Result.Success(new TenantBillingOverviewResponse(
            PaymentStatus: tenant.PaymentStatus,
            SubscriptionStatus: overview.SubscriptionStatus,
            PlanId: tenant.PlanId,
            PlanName: planName,
            StripeCustomerId: tenant.StripeCustomerId,
            StripeSubscriptionId: tenant.StripeSubscriptionId,
            LastPaymentDate: tenant.LastPaymentDate,
            NextPaymentDate: overview.NextPaymentDate,
            GracePeriodEndsAt: overview.GracePeriodEndsAt,
            AvailabilityEndsAt: overview.AvailabilityEndsAt,
            HasRecurringPayment: overview.HasRecurringPayment,
            CanManageBilling: !string.IsNullOrWhiteSpace(tenant.StripeCustomerId),
            RecentTransactions: overview.RecentTransactions));
    }
}
