namespace Homeless.Application.Sagas.Payment;

public sealed record CheckoutSessionRequested
{
    public Guid CorrelationId { get; init; }
    public Guid TenantId { get; init; }
    public Guid PlanId { get; init; }
    public long Amount { get; init; }
    public string Currency { get; init; } = string.Empty;
    public string SuccessUrl { get; init; } = string.Empty;
    public string CancelUrl { get; init; } = string.Empty;
}

public sealed record CheckoutSessionCreated
{
    public Guid CorrelationId { get; init; }
    public string StripeSessionId { get; init; } = string.Empty;
    public string SessionUrl { get; init; } = string.Empty;
    public string PaymentIntentId { get; init; } = string.Empty;
}

public sealed record CheckoutSessionFailed
{
    public Guid CorrelationId { get; init; }
    public string Reason { get; init; } = string.Empty;
}

public sealed record PaymentWebhookReceived
{
    public Guid CorrelationId { get; init; }
    public string EventId { get; init; } = string.Empty;
    public string EventType { get; init; } = string.Empty;
    public string SessionId { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
}

public sealed record SubscriptionActivated
{
    public Guid CorrelationId { get; init; }
    public Guid TenantId { get; init; }
    public Guid PlanId { get; init; }
}

public sealed record SubscriptionActivationFailed
{
    public Guid CorrelationId { get; init; }
    public Guid TenantId { get; init; }
    public string Reason { get; init; } = string.Empty;
}

public sealed record PaymentSagaCompleted
{
    public Guid CorrelationId { get; init; }
    public Guid TenantId { get; init; }
    public Guid PlanId { get; init; }
    public string StripeSessionId { get; init; } = string.Empty;
}

public sealed record PaymentSagaFailed
{
    public Guid CorrelationId { get; init; }
    public Guid TenantId { get; init; }
    public string Reason { get; init; } = string.Empty;
}

public sealed record PaymentSessionTimedOut
{
    public Guid CorrelationId { get; init; }
}
