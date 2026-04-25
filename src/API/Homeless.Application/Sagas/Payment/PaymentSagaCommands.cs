namespace Homeless.Application.Sagas.Payment;

public sealed record CreateCheckoutSessionCommand
{
    public Guid CorrelationId { get; init; }
    public Guid TenantId { get; init; }
    public Guid PlanId { get; init; }
    public long Amount { get; init; }
    public string Currency { get; init; } = string.Empty;
    public string SuccessUrl { get; init; } = string.Empty;
    public string CancelUrl { get; init; } = string.Empty;
}

public sealed record ActivateSubscriptionCommand
{
    public Guid CorrelationId { get; init; }
    public Guid TenantId { get; init; }
    public Guid PlanId { get; init; }
}
