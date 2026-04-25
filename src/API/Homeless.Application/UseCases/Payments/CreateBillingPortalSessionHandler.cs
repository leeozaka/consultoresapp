using Ardalis.Result;
using MediatR;
using Homeless.Application.DTOs;
using Homeless.Application.Interfaces;
using Homeless.Domain.Interfaces.Repositories.Read;

namespace Homeless.Application.UseCases.Payments;

public sealed class CreateBillingPortalSessionHandler(
    ITenantReadRepository tenantReadRepository,
    ITenantContext tenantContext,
    IPaymentGateway paymentGateway)
    : IRequestHandler<CreateBillingPortalSessionCommand, Result<BillingPortalSessionResponse>>
{
    public async Task<Result<BillingPortalSessionResponse>> Handle(
        CreateBillingPortalSessionCommand request,
        CancellationToken cancellationToken)
    {
        if (!tenantContext.IsResolved)
            return Result.NotFound("No tenant resolved for this request.");

        var tenant = await tenantReadRepository
            .GetByIdAsync(tenantContext.TenantId, cancellationToken)
            .ConfigureAwait(false);

        if (tenant is null)
            return Result.NotFound("Tenant not found.");

        if (string.IsNullOrWhiteSpace(tenant.StripeCustomerId))
            return Result.Error("This tenant does not have a Stripe customer registered yet.");

        var session = await paymentGateway
            .CreateBillingPortalSessionAsync(tenant.StripeCustomerId, request.ReturnUrl, cancellationToken)
            .ConfigureAwait(false);

        return Result.Success(new BillingPortalSessionResponse(session.Url));
    }
}
