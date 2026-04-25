using Homeless.Application.DTOs;

namespace Homeless.Application.Interfaces;

public interface IPaymentEventStream
{
    ValueTask PublishAsync(
        PaymentEventResponse paymentEvent,
        CancellationToken cancellationToken = default
    );
    IAsyncEnumerable<PaymentEventResponse> ReadAllAsync(
        CancellationToken cancellationToken = default
    );
}
