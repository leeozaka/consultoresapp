using System.Diagnostics.CodeAnalysis;
using Ardalis.Result;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using Homeless.Application.DTOs;
using Homeless.Application.Interfaces;
using Homeless.Application.Sagas.Payment;
using Homeless.Application.UseCases.Onboarding;
using Homeless.Domain.Entities;
using Homeless.Domain.Enums;
using Homeless.Domain.Interfaces.Repositories.Read;
using Homeless.Domain.Interfaces.Repositories.Write;
using Homeless.Domain.ValueObjects;

namespace Homeless.Application.UseCases.Webhooks;

public sealed class ProcessStripeWebhookHandler(
    IPaymentGateway paymentGateway,
    ICacheService cacheService,
    IKeyValueStore keyValueStore,
    IPaymentEventStream paymentEventStream,
    IOnboardingStatusStream onboardingStatusStream,
    ITenantReadRepository tenantReadRepository,
    ITenantWriteRepository tenantWriteRepository,
    IIdentityService identityService,
    ISiteBuildChannel siteBuildChannel,
    IEventBus eventBus,
    IUnitOfWork unitOfWork,
    IBus bus,
    ILogger<ProcessStripeWebhookHandler> logger)
    : IRequestHandler<ProcessStripeWebhookCommand, Result>
{
    public async Task<Result> Handle(ProcessStripeWebhookCommand request, CancellationToken cancellationToken)
    {
        var webhookEvent = request.Event;

        logger.LogInformation(
            "Processing Stripe webhook {EventType} (EventId={EventId})",
            webhookEvent.EventType, webhookEvent.EventId);

        switch (webhookEvent.EventType)
        {
            case "checkout.session.completed":
                await HandleCheckoutCompletedAsync(webhookEvent, cancellationToken).ConfigureAwait(false);
                await PublishWebhookToSagaAsync(webhookEvent, cancellationToken).ConfigureAwait(false);
                break;

            case "checkout.session.expired":
                await PublishWebhookToSagaAsync(webhookEvent, cancellationToken).ConfigureAwait(false);
                break;

            case "invoice.paid":
                await HandleInvoicePaidAsync(webhookEvent, cancellationToken).ConfigureAwait(false);
                break;

            case "invoice.payment_failed":
                await HandleInvoicePaymentFailedAsync(webhookEvent, cancellationToken).ConfigureAwait(false);
                break;

            case "customer.subscription.created":
            case "customer.subscription.updated":
                await HandleSubscriptionUpdatedAsync(webhookEvent, cancellationToken).ConfigureAwait(false);
                break;

            case "customer.subscription.deleted":
                await HandleSubscriptionDeletedAsync(webhookEvent, cancellationToken).ConfigureAwait(false);
                break;

            default:
                logger.LogInformation(
                    "Stripe webhook event {EventType} (EventId={EventId}) has no handler — ignored",
                    webhookEvent.EventType, webhookEvent.EventId);
                break;
        }

        await PublishSseEventAsync(webhookEvent, cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Stripe webhook {EventType} (EventId={EventId}) processed successfully",
            webhookEvent.EventType, webhookEvent.EventId);

        return Result.Success();
    }

    private async Task PublishWebhookToSagaAsync(
        PaymentWebhookEvent webhookEvent,
        CancellationToken cancellationToken)
    {
        var correlationId = await ResolveCorrelationIdAsync(webhookEvent, cancellationToken).ConfigureAwait(false);
        if (correlationId is null)
            return;

        await bus.Publish(new PaymentWebhookReceived
        {
            CorrelationId = correlationId.Value,
            EventId = webhookEvent.EventId,
            EventType = webhookEvent.EventType,
            SessionId = webhookEvent.SessionId,
            Status = webhookEvent.Status
        }, cancellationToken).ConfigureAwait(false);
    }

    private async Task HandleCheckoutCompletedAsync(
        PaymentWebhookEvent webhookEvent,
        CancellationToken cancellationToken)
    {
        var tenant = await ResolveTenantAsync(webhookEvent, cancellationToken).ConfigureAwait(false);
        if (tenant is null) return;

        await UpdateTenantAfterCheckoutAsync(tenant, webhookEvent, cancellationToken).ConfigureAwait(false);
        await TryAutoActivateSignupAsync(tenant, webhookEvent, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// If this is a signup checkout (marker stored by SignupHandler), conditionally activates
    /// the tenant and its users:
    /// - Starter plan (no custom_domain): auto-activate + trigger site build.
    /// - Professional/Enterprise plan (custom_domain): stay Pending, notify SuperAdmin queue.
    /// </summary>
    private async Task TryAutoActivateSignupAsync(
        Tenant tenant,
        PaymentWebhookEvent webhookEvent,
        CancellationToken cancellationToken)
    {
        var signupKey = SignupHandler.SignupCheckoutKey(webhookEvent.SessionId);
        var tenantIdStr = await keyValueStore
            .GetAsync<string>(signupKey, cancellationToken)
            .ConfigureAwait(false);

        if (tenantIdStr is null)
            return; // Not a signup checkout

        await keyValueStore.RemoveAsync(signupKey, cancellationToken).ConfigureAwait(false);

        if (!tenant.HasEntitlement("custom_domain"))
        {
            tenant.Activate();

            await tenantWriteRepository.UpdateAsync(tenant, cancellationToken).ConfigureAwait(false);
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await identityService
                .ActivateUsersByTenantAsync(tenant.Id, cancellationToken)
                .ConfigureAwait(false);

            await siteBuildChannel.WriteAsync(
                new SiteBuildMessage(tenant.Id, tenant.Slug, tenant.CustomDomain, tenant.FrontendOrigin),
                cancellationToken).ConfigureAwait(false);

            await onboardingStatusStream.PublishAsync(new OnboardingStatusEvent(
                TenantId: tenant.Id,
                Status: "provisioning",
                Message: "Payment confirmed — setting up your portal…"),
                cancellationToken).ConfigureAwait(false);

            logger.LogInformation(
                "Signup tenant {TenantId} auto-activated after Starter plan checkout", tenant.Id);
        }
        else
        {
            await onboardingStatusStream.PublishAsync(new OnboardingStatusEvent(
                TenantId: tenant.Id,
                Status: "awaiting_approval",
                Message: "Payment confirmed — your account is pending SuperAdmin approval."),
                cancellationToken).ConfigureAwait(false);

            logger.LogInformation(
                "Signup tenant {TenantId} awaiting SuperAdmin approval (plan with custom_domain)", tenant.Id);
        }
    }

    private async Task UpdateTenantAfterCheckoutAsync(
        Tenant tenant,
        PaymentWebhookEvent webhookEvent,
        CancellationToken cancellationToken)
    {
        var nextBilling = webhookEvent.CurrentPeriodEnd ?? tenant.NextBillingDate ?? DateTime.UtcNow.AddMonths(1);
        tenant.RecordPaymentSucceeded(DateTime.UtcNow, nextBilling);

        if (!string.IsNullOrEmpty(webhookEvent.CustomerId))
            tenant.SetStripeCustomerId(webhookEvent.CustomerId);

        if (!string.IsNullOrEmpty(webhookEvent.SubscriptionId))
            tenant.SetStripeSubscriptionId(webhookEvent.SubscriptionId);

        await tenantWriteRepository.UpdateAsync(tenant, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task HandleInvoicePaidAsync(
        PaymentWebhookEvent webhookEvent,
        CancellationToken cancellationToken)
    {
        var tenantId = ResolveTenantId(webhookEvent);
        if (tenantId is null) return;

        var tenant = await tenantReadRepository
            .GetByIdAsync(tenantId.Value, cancellationToken).ConfigureAwait(false);

        if (tenant is null) return;

        var nextBilling = webhookEvent.CurrentPeriodEnd ?? tenant.NextBillingDate ?? DateTime.UtcNow.AddMonths(1);
        tenant.RecordPaymentSucceeded(DateTime.UtcNow, nextBilling);
        tenant.SyncRecurringBilling(
            webhookEvent.CustomerId,
            string.IsNullOrWhiteSpace(webhookEvent.SubscriptionId) ? tenant.StripeSubscriptionId : webhookEvent.SubscriptionId,
            paymentStatus: "paid",
            nextBillingDate: nextBilling);
        await tenantWriteRepository.UpdateAsync(tenant, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Invoice paid for tenant {TenantId}, next billing {NextBilling}",
            tenantId, nextBilling);
    }

    private async Task HandleInvoicePaymentFailedAsync(
        PaymentWebhookEvent webhookEvent,
        CancellationToken cancellationToken)
    {
        var tenantId = ResolveTenantId(webhookEvent);
        if (tenantId is null) return;

        var tenant = await tenantReadRepository
            .GetByIdAsync(tenantId.Value, cancellationToken).ConfigureAwait(false);

        if (tenant is null) return;

        tenant.RecordPaymentFailed();
        tenant.SyncRecurringBilling(
            webhookEvent.CustomerId,
            string.IsNullOrWhiteSpace(webhookEvent.SubscriptionId) ? tenant.StripeSubscriptionId : webhookEvent.SubscriptionId,
            paymentStatus: "past_due",
            nextBillingDate: webhookEvent.NextPaymentAttemptAt ?? tenant.NextBillingDate);

        if (tenant.IsPaymentOverdue())
        {
            tenant.Suspend("Payment overdue — suspended after 7-day grace period.");
            logger.LogWarning("Tenant {TenantId} suspended due to overdue payment", tenantId);
        }

        await tenantWriteRepository.UpdateAsync(tenant, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogWarning("Invoice payment failed for tenant {TenantId}", tenantId);
    }

    private async Task HandleSubscriptionUpdatedAsync(
        PaymentWebhookEvent webhookEvent,
        CancellationToken cancellationToken)
    {
        var tenantId = ResolveTenantId(webhookEvent);
        if (tenantId is null) return;

        var tenant = await tenantReadRepository
            .GetByIdAsync(tenantId.Value, cancellationToken).ConfigureAwait(false);

        if (tenant is null) return;

        tenant.SyncRecurringBilling(
            webhookEvent.CustomerId,
            webhookEvent.SubscriptionId,
            paymentStatus: string.IsNullOrWhiteSpace(webhookEvent.SubscriptionStatus)
                ? webhookEvent.Status
                : webhookEvent.SubscriptionStatus,
            nextBillingDate: webhookEvent.CurrentPeriodEnd ?? tenant.NextBillingDate);

        await tenantWriteRepository.UpdateAsync(tenant, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task HandleSubscriptionDeletedAsync(
        PaymentWebhookEvent webhookEvent,
        CancellationToken cancellationToken)
    {
        var tenantId = ResolveTenantId(webhookEvent);
        if (tenantId is null) return;

        var tenant = await tenantReadRepository
            .GetByIdAsync(tenantId.Value, cancellationToken).ConfigureAwait(false);

        if (tenant is null) return;

        tenant.MarkSubscriptionCanceled(webhookEvent.CurrentPeriodEnd ?? tenant.NextBillingDate);

        await tenantWriteRepository.UpdateAsync(tenant, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<Tenant?> ResolveTenantAsync(
        PaymentWebhookEvent webhookEvent,
        CancellationToken cancellationToken)
    {
        var tenantId = ResolveTenantId(webhookEvent);
        if (tenantId is null)
            return null;

        return await tenantReadRepository
            .GetByIdAsync(tenantId.Value, cancellationToken).ConfigureAwait(false);
    }

    private async Task<Guid?> ResolveCorrelationIdAsync(
        PaymentWebhookEvent webhookEvent,
        CancellationToken cancellationToken)
    {
        var correlationIdStr = webhookEvent.Metadata?.GetValueOrDefault("correlation_id");
        if (correlationIdStr is not null && Guid.TryParse(correlationIdStr, out var correlationId))
            return correlationId;

        var storedCorrelationId = await keyValueStore.GetAsync<string>(
            $"stripe-session:{webhookEvent.SessionId}", cancellationToken).ConfigureAwait(false);

        if (storedCorrelationId is not null && Guid.TryParse(storedCorrelationId, out var resolvedId))
            return resolvedId;

        logger.LogWarning("Cannot resolve saga correlation for session {SessionId}", webhookEvent.SessionId);
        return null;
    }

    private static Guid? ResolveTenantId(PaymentWebhookEvent webhookEvent)
    {
        var tenantIdStr = webhookEvent.Metadata?.GetValueOrDefault("tenant_id");
        return tenantIdStr is not null && Guid.TryParse(tenantIdStr, out var tid) ? tid : null;
    }

    private async Task PublishSseEventAsync(
        PaymentWebhookEvent webhookEvent,
        CancellationToken cancellationToken)
    {
        var tenantName = webhookEvent.Metadata?.GetValueOrDefault("tenant_id") ?? "Unknown";
        var statusLabel = webhookEvent.EventType switch
        {
            "checkout.session.completed" => "payment_succeeded",
            "checkout.session.expired" => "payment_expired",
            "invoice.paid" => "invoice_paid",
            "invoice.payment_failed" => "invoice_failed",
            "customer.subscription.created" => "subscription_created",
            "customer.subscription.updated" => "subscription_updated",
            "customer.subscription.deleted" => "subscription_canceled",
            _ => webhookEvent.EventType
        };

        await paymentEventStream.PublishAsync(new PaymentEventResponse(
            TenantId: ResolveTenantId(webhookEvent) ?? Guid.Empty,
            TenantName: tenantName,
            PlanName: webhookEvent.Metadata?.GetValueOrDefault("plan_id") ?? "",
            Amount: webhookEvent.AmountTotal,
            Currency: webhookEvent.Currency,
            Status: statusLabel,
            OccurredAt: DateTime.UtcNow,
            Detail: webhookEvent.Description,
            NextPaymentDate: webhookEvent.CurrentPeriodEnd ?? webhookEvent.NextPaymentAttemptAt,
            SubscriptionStatus: string.IsNullOrWhiteSpace(webhookEvent.SubscriptionStatus)
                ? webhookEvent.Status
                : webhookEvent.SubscriptionStatus), cancellationToken).ConfigureAwait(false);
    }
}
