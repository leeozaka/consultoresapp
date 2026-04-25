using Homeless.Application.DTOs;

namespace Homeless.Application.Interfaces;

public interface IPaymentGateway
{
    Task<CheckoutSessionResult> CreateCheckoutSessionAsync(
        Guid tenantId,
        Guid planId,
        long amount,
        string currency,
        string successUrl,
        string cancelUrl,
        CancellationToken cancellationToken = default
    );

    Task<CheckoutSessionResult> CreateSubscriptionCheckoutSessionAsync(
        RecurringSubscriptionCheckoutRequest request,
        CancellationToken cancellationToken = default
    );

    Task<BillingOverviewResult> GetBillingOverviewAsync(
        BillingOverviewRequest request,
        CancellationToken cancellationToken = default
    );

    Task<BillingPortalSessionResult> CreateBillingPortalSessionAsync(
        string customerId,
        string returnUrl,
        CancellationToken cancellationToken = default
    );

    Task<BillingSubscriptionUpdateResult> UpdateSubscriptionAsync(
        string subscriptionId,
        string priceId,
        bool prorate,
        CancellationToken cancellationToken = default
    );

    bool VerifyWebhookSignature(byte[] payload, string signatureHeader, string secret);

    PaymentWebhookEvent ParseWebhookEvent(byte[] payload);
}
