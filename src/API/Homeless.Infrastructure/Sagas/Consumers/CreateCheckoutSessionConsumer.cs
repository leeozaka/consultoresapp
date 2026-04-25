using Homeless.Application.DTOs;
using Homeless.Application.Interfaces;
using Homeless.Application.Sagas.Payment;
using Homeless.Domain.Interfaces.Repositories.Read;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Homeless.Infrastructure.Sagas.Consumers;

public sealed class CreateCheckoutSessionConsumer(
    IPaymentGateway paymentGateway,
    IKeyValueStore keyValueStore,
    ITenantReadRepository tenantReadRepository,
    IPlanReadRepository planReadRepository,
    ILogger<CreateCheckoutSessionConsumer> logger
) : IConsumer<CreateCheckoutSessionCommand>
{
    public async Task Consume(ConsumeContext<CreateCheckoutSessionCommand> context)
    {
        var cmd = context.Message;

        try
        {
            logger.LogInformation(
                "Creating checkout session for tenant {TenantId}, plan {PlanId}",
                cmd.TenantId,
                cmd.PlanId
            );

            var tenant = await tenantReadRepository
                .GetByIdAsync(cmd.TenantId, context.CancellationToken)
                .ConfigureAwait(false);
            var plan = await planReadRepository
                .GetByIdAsync(cmd.PlanId, context.CancellationToken)
                .ConfigureAwait(false);

            if (tenant is null || plan is null)
                throw new InvalidOperationException(
                    "Tenant or plan not found while creating Stripe subscription checkout."
                );

            var result = await paymentGateway
                .CreateSubscriptionCheckoutSessionAsync(
                    new RecurringSubscriptionCheckoutRequest(
                        TenantId: tenant.Id,
                        TenantName: tenant.Name,
                        CustomerEmail: tenant.ContactEmail,
                        PlanId: plan.Id,
                        PlanName: plan.Name,
                        Amount: cmd.Amount,
                        Currency: cmd.Currency,
                        SuccessUrl: cmd.SuccessUrl,
                        CancelUrl: cmd.CancelUrl,
                        StripePriceId: plan.StripePriceId,
                        StripeCustomerId: tenant.StripeCustomerId,
                        CorrelationId: cmd.CorrelationId
                    ),
                    context.CancellationToken
                )
                .ConfigureAwait(false);

            await keyValueStore
                .SetAsync(
                    $"stripe-session:{result.SessionId}",
                    cmd.CorrelationId.ToString(),
                    context.CancellationToken
                )
                .ConfigureAwait(false);

            await context
                .Publish(
                    new CheckoutSessionCreated
                    {
                        CorrelationId = cmd.CorrelationId,
                        StripeSessionId = result.SessionId,
                        SessionUrl = result.SessionUrl,
                        PaymentIntentId = result.PaymentIntentId,
                    }
                )
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to create checkout session for tenant {TenantId}",
                cmd.TenantId
            );

            await context
                .Publish(
                    new CheckoutSessionFailed
                    {
                        CorrelationId = cmd.CorrelationId,
                        Reason = ex.Message,
                    }
                )
                .ConfigureAwait(false);
        }
    }
}
