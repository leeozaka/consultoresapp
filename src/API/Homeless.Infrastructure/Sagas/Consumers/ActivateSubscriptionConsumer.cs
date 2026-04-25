using Homeless.Application.DTOs;
using Homeless.Application.Interfaces;
using Homeless.Application.Sagas.Payment;
using Homeless.Domain.Interfaces.Repositories.Read;
using Homeless.Domain.Interfaces.Repositories.Write;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Homeless.Infrastructure.Sagas.Consumers;

public sealed class ActivateSubscriptionConsumer(
    ITenantReadRepository tenantReadRepository,
    ITenantWriteRepository tenantWriteRepository,
    IPlanReadRepository planReadRepository,
    IUnitOfWork unitOfWork,
    IPaymentEventStream paymentEventStream,
    ILogger<ActivateSubscriptionConsumer> logger
) : IConsumer<ActivateSubscriptionCommand>
{
    public async Task Consume(ConsumeContext<ActivateSubscriptionCommand> context)
    {
        var cmd = context.Message;

        logger.LogInformation(
            "Activating subscription for tenant {TenantId} with plan {PlanId}",
            cmd.TenantId,
            cmd.PlanId
        );

        var tenant = await tenantReadRepository
            .GetByIdAsync(cmd.TenantId, context.CancellationToken)
            .ConfigureAwait(false);

        if (tenant is null)
        {
            var reason = $"Tenant {cmd.TenantId} not found during subscription activation";
            logger.LogWarning("{Reason}", reason);
            await context
                .Publish(
                    new SubscriptionActivationFailed
                    {
                        CorrelationId = cmd.CorrelationId,
                        TenantId = cmd.TenantId,
                        Reason = reason,
                    }
                )
                .ConfigureAwait(false);
            return;
        }

        var plan = await planReadRepository
            .GetByIdAsync(cmd.PlanId, context.CancellationToken)
            .ConfigureAwait(false);

        if (plan is null)
        {
            var reason = $"Plan {cmd.PlanId} not found during subscription activation";
            logger.LogWarning("{Reason}", reason);
            await context
                .Publish(
                    new SubscriptionActivationFailed
                    {
                        CorrelationId = cmd.CorrelationId,
                        TenantId = cmd.TenantId,
                        Reason = reason,
                    }
                )
                .ConfigureAwait(false);
            return;
        }

        tenant.ChangePlan(plan);
        await tenantWriteRepository
            .UpdateAsync(tenant, context.CancellationToken)
            .ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(context.CancellationToken).ConfigureAwait(false);

        await paymentEventStream
            .PublishAsync(
                new PaymentEventResponse(
                    TenantId: cmd.TenantId,
                    TenantName: tenant.Name,
                    PlanName: plan.Name,
                    Amount: 0,
                    Currency: "BRL",
                    Status: "activated",
                    OccurredAt: DateTime.UtcNow
                ),
                context.CancellationToken
            )
            .ConfigureAwait(false);

        await context
            .Publish(
                new SubscriptionActivated
                {
                    CorrelationId = cmd.CorrelationId,
                    TenantId = cmd.TenantId,
                    PlanId = cmd.PlanId,
                }
            )
            .ConfigureAwait(false);
    }
}
