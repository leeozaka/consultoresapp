namespace Homeless.Application.DTOs;

public sealed record CheckoutSessionResult(
    string SessionId,
    string SessionUrl,
    string PaymentIntentId,
    string CustomerId = "",
    string SubscriptionId = ""
);

public sealed record RecurringSubscriptionCheckoutRequest(
    Guid TenantId,
    string TenantName,
    string CustomerEmail,
    Guid PlanId,
    string PlanName,
    long Amount,
    string Currency,
    string SuccessUrl,
    string CancelUrl,
    string? StripePriceId = null,
    string? StripeCustomerId = null,
    long? OneTimeAmount = null,
    string? OneTimeDescription = null,
    Guid? CorrelationId = null
);

public sealed record PaymentWebhookEvent(
    string EventId,
    string EventType,
    string SessionId,
    string PaymentIntentId,
    string SubscriptionId,
    string Status,
    long AmountTotal,
    string Currency,
    Dictionary<string, string>? Metadata,
    string CustomerId = "",
    string InvoiceId = "",
    string SubscriptionStatus = "",
    string BillingReason = "",
    DateTime? CurrentPeriodEnd = null,
    DateTime? NextPaymentAttemptAt = null,
    string HostedInvoiceUrl = "",
    string InvoicePdfUrl = "",
    string Description = ""
);

public sealed record BillingOverviewRequest(
    Guid TenantId,
    string? StripeCustomerId,
    string? StripeSubscriptionId,
    string PaymentStatus,
    DateTime? LastPaymentDate,
    DateTime? NextBillingDate,
    int RecentTransactionsLimit = 10
);

public sealed record BillingTransactionResult(
    string Id,
    string Type,
    string Status,
    long Amount,
    string Currency,
    DateTime OccurredAt,
    string Description,
    string? HostedInvoiceUrl = null
);

public sealed record BillingOverviewResult(
    string SubscriptionStatus,
    bool HasRecurringPayment,
    DateTime? NextPaymentDate,
    DateTime? GracePeriodEndsAt,
    DateTime? AvailabilityEndsAt,
    IReadOnlyList<BillingTransactionResult> RecentTransactions
);

public sealed record BillingPortalSessionResult(string Url);

public sealed record BillingPortalSessionResponse(string Url);

public sealed record BillingSubscriptionUpdateResult(
    string SubscriptionId,
    string SubscriptionStatus,
    DateTime? CurrentPeriodEnd
);

public sealed record TenantBillingOverviewResponse(
    string PaymentStatus,
    string SubscriptionStatus,
    Guid? PlanId,
    string? PlanName,
    string? StripeCustomerId,
    string? StripeSubscriptionId,
    DateTime? LastPaymentDate,
    DateTime? NextPaymentDate,
    DateTime? GracePeriodEndsAt,
    DateTime? AvailabilityEndsAt,
    bool HasRecurringPayment,
    bool CanManageBilling,
    IReadOnlyList<BillingTransactionResult> RecentTransactions
);

public sealed record CreateBillingPortalSessionRequest(string ReturnUrl);

public sealed record PaymentEventResponse(
    Guid TenantId,
    string TenantName,
    string PlanName,
    long Amount,
    string Currency,
    string Status,
    DateTime OccurredAt,
    string? Detail = null,
    DateTime? NextPaymentDate = null,
    string? SubscriptionStatus = null
);
