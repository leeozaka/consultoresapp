using MassTransit;
using Microsoft.Extensions.Logging;
using Homeless.Application.Interfaces;
using Homeless.Application.Sagas.Payment;
using Homeless.Application.UseCases.Onboarding;
using Homeless.Domain.Enums;
using Homeless.Domain.Interfaces.Repositories.Read;
using Homeless.Domain.Interfaces.Repositories.Write;

namespace Homeless.Infrastructure.Sagas.Consumers;

public sealed class PaymentSagaFailedConsumer(
    ITenantReadRepository tenantReadRepository,
    ITenantWriteRepository tenantWriteRepository,
    IIdentityService identityService,
    IOnboardingStatusStream onboardingStatusStream,
    IUnitOfWork unitOfWork,
    ILogger<PaymentSagaFailedConsumer> logger)
    : IConsumer<PaymentSagaFailed>
{
    public async Task Consume(ConsumeContext<PaymentSagaFailed> context)
    {
        var tenantId = context.Message.TenantId;

        logger.LogInformation(
            "PaymentSagaFailed received for tenant {TenantId}. Reason: {Reason}",
            tenantId, context.Message.Reason);

        var tenant = await tenantReadRepository
            .GetByIdAsync(tenantId, context.CancellationToken)
            .ConfigureAwait(false);

        // Guard: only clean up signup-abandoned tenants.
        // Plan-change flows always involve Active tenants with a non-"none" PaymentStatus.
        if (tenant is null
            || tenant.Status != TenantStatus.Pending
            || tenant.PaymentStatus != "none")
        {
            if (tenant is null)
                logger.LogInformation("Tenant {TenantId} not found — already cleaned up, no-op.", tenantId);
            else
                logger.LogInformation(
                    "Tenant {TenantId} is not a signup-abandoned tenant (Status={Status}, PaymentStatus={PaymentStatus}). Skipping cleanup.",
                    tenantId, tenant.Status, tenant.PaymentStatus);
            return;
        }

        logger.LogInformation("Cleaning up abandoned signup for tenant {TenantId}.", tenantId);

        // Notify SSE clients BEFORE deletion so any open status page can react.
        // The SSE stream buffers events per-tenant with a 30s replay window, so the
        // event will be delivered even if the tenant record is deleted immediately after.
        await onboardingStatusStream.PublishAsync(new OnboardingStatusEvent(
            TenantId: tenantId,
            Status: "abandoned",
            Message: "O pagamento não foi concluído. O cadastro foi removido."),
            context.CancellationToken).ConfigureAwait(false);

        if (tenant.OwnerUserId.HasValue)
        {
            await identityService
                .DeleteUserAsync(tenant.OwnerUserId.Value, context.CancellationToken)
                .ConfigureAwait(false);

            logger.LogInformation("Deleted user {UserId} for abandoned tenant {TenantId}.",
                tenant.OwnerUserId.Value, tenantId);
        }

        await tenantWriteRepository
            .DeleteAsync(tenant.Id, context.CancellationToken)
            .ConfigureAwait(false);

        await unitOfWork.SaveChangesAsync(context.CancellationToken).ConfigureAwait(false);

        logger.LogInformation("Abandoned tenant {TenantId} deleted successfully.", tenantId);
    }
}
