namespace Homeless.Application.Sagas.Payment;

public sealed record PaymentSagaResult(
    bool Success,
    string? SessionUrl,
    string? StripeSessionId,
    string? ErrorMessage
);

public interface IPaymentSagaService
{
    Task<PaymentSagaResult> InitiateCheckoutAsync(
        Guid tenantId,
        Guid planId,
        long amount,
        string currency,
        string successUrl,
        string cancelUrl,
        CancellationToken cancellationToken = default
    );
}
