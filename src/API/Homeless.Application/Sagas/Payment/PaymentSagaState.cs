using MassTransit;

namespace Homeless.Application.Sagas.Payment;

public sealed class PaymentSagaState : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    public string CurrentState { get; set; } = string.Empty;

    public Guid TenantId { get; set; }
    public Guid PlanId { get; set; }
    public long Amount { get; set; }
    public string Currency { get; set; } = string.Empty;

    public string? StripeSessionId { get; set; }
    public string? PaymentIntentId { get; set; }
    public string? SessionUrl { get; set; }
    public string? FailureReason { get; set; }

    /// <summary>Quartz schedule token for the 25-hour checkout expiration timeout.</summary>
    public Guid? CheckoutExpirationTokenId { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
