using System.Diagnostics.CodeAnalysis;
using Ardalis.Result;
using Homeless.Application.DTOs;
using Homeless.Application.Interfaces;
using Homeless.Application.Mappers;
using Homeless.Application.Sagas.Payment;
using Homeless.Domain.Enums;
using Homeless.Domain.Interfaces.Repositories.Read;
using Homeless.Domain.Interfaces.Repositories.Write;
using MediatR;

namespace Homeless.Application.UseCases.Plans;

[SuppressMessage(
    "Maintainability",
    "S107:Methods should not have too many parameters",
    Justification = "Billing plan changes coordinate repositories, checkout orchestration, cache invalidation, tenant context, and Stripe integration."
)]
public sealed class ChangeTenantPlanHandler(
    IPlanReadRepository planReadRepository,
    ITenantReadRepository tenantReadRepository,
    ITenantWriteRepository tenantWriteRepository,
    IPaymentSagaService paymentSagaService,
    IPaymentGateway paymentGateway,
    ICacheService cacheService,
    ITenantContext tenantContext,
    IUnitOfWork unitOfWork
) : IRequestHandler<ChangeTenantPlanCommand, Result<ChangePlanResponse>>
{
    public async Task<Result<ChangePlanResponse>> Handle(
        ChangeTenantPlanCommand request,
        CancellationToken cancellationToken
    )
    {
        var newPlan = await planReadRepository
            .GetByIdAsync(request.PlanId, cancellationToken)
            .ConfigureAwait(false);

        if (newPlan is null || !newPlan.IsActive)
            return Result.NotFound($"Plan '{request.PlanId}' not found or is no longer available.");

        var tenant = await tenantReadRepository
            .GetByIdAsync(tenantContext.TenantId, cancellationToken)
            .ConfigureAwait(false);

        if (tenant is null)
            return Result.NotFound("Tenant not found.");

        if (tenant.Status != TenantStatus.Active)
            return Result.Error("Only active tenants can change their subscription plan.");

        var isRetryingCurrentPlan = tenant.PlanId == newPlan.Id;
        var canRetryCurrentPlanCheckout =
            isRetryingCurrentPlan && tenant.HasPendingPayment && !tenant.HasRecurringPayment;

        if (isRetryingCurrentPlan && !canRetryCurrentPlanCheckout)
            return Result.Error("Tenant is already on this plan.");

        var currentPriceCents = await GetCurrentPriceCentsAsync(tenant.PlanId, cancellationToken)
            .ConfigureAwait(false);
        var isUpgrade = newPlan.Price.AmountInCents > currentPriceCents;

        if (tenant.HasRecurringPayment && !string.IsNullOrWhiteSpace(tenant.StripeSubscriptionId))
        {
            if (string.IsNullOrWhiteSpace(newPlan.StripePriceId))
                return Result.Error("Selected plan is not mapped to a Stripe recurring price.");

            var subscription = await paymentGateway
                .UpdateSubscriptionAsync(
                    tenant.StripeSubscriptionId!,
                    newPlan.StripePriceId,
                    prorate: isUpgrade,
                    cancellationToken
                )
                .ConfigureAwait(false);

            tenant.ChangePlan(newPlan);
            tenant.SyncRecurringBilling(
                tenant.StripeCustomerId,
                subscription.SubscriptionId,
                subscription.SubscriptionStatus,
                subscription.CurrentPeriodEnd
            );

            await tenantWriteRepository
                .UpdateAsync(tenant, cancellationToken)
                .ConfigureAwait(false);
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await cacheService
                .RemoveAsync(
                    CacheKeys.TenantEntitlements(tenantContext.TenantId),
                    cancellationToken
                )
                .ConfigureAwait(false);

            return Result.Success(
                new ChangePlanResponse(
                    Plan: newPlan.ToResponse(),
                    RequiresCheckout: false,
                    CheckoutUrl: null,
                    UpdatedInPlace: true,
                    Message: "Subscription updated successfully."
                )
            );
        }

        if (isUpgrade || canRetryCurrentPlanCheckout)
        {
            var result = await paymentSagaService
                .InitiateCheckoutAsync(
                    tenantContext.TenantId,
                    newPlan.Id,
                    newPlan.Price.AmountInCents,
                    newPlan.Price.Currency.Code,
                    successUrl: request.SuccessUrl ?? "/billing/success",
                    cancelUrl: request.CancelUrl ?? "/billing/cancel",
                    cancellationToken
                )
                .ConfigureAwait(false);

            if (!result.Success)
                return Result.Error($"Payment initiation failed: {result.ErrorMessage}");

            return Result.Success(
                new ChangePlanResponse(
                    Plan: newPlan.ToResponse(),
                    RequiresCheckout: true,
                    CheckoutUrl: result.SessionUrl,
                    UpdatedInPlace: false,
                    Message: "Checkout session created successfully."
                )
            );
        }

        tenant.ChangePlan(newPlan);

        await tenantWriteRepository.UpdateAsync(tenant, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await cacheService
            .RemoveAsync(CacheKeys.TenantEntitlements(tenantContext.TenantId), cancellationToken)
            .ConfigureAwait(false);

        return Result.Success(
            new ChangePlanResponse(
                Plan: newPlan.ToResponse(),
                RequiresCheckout: false,
                CheckoutUrl: null,
                UpdatedInPlace: true,
                Message: "Plan updated successfully."
            )
        );
    }

    private async Task<long> GetCurrentPriceCentsAsync(
        Guid? currentPlanId,
        CancellationToken cancellationToken
    )
    {
        if (!currentPlanId.HasValue)
            return 0L;

        var current = await planReadRepository
            .GetByIdAsync(currentPlanId.Value, cancellationToken)
            .ConfigureAwait(false);

        return current?.Price.AmountInCents ?? 0L;
    }
}
