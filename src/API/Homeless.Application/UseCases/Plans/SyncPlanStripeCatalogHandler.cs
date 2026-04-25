using Ardalis.Result;
using MediatR;
using Homeless.Application.DTOs;
using Homeless.Application.Interfaces;
using Homeless.Application.Mappers;
using Homeless.Domain.Interfaces.Repositories.Read;
using Homeless.Domain.Interfaces.Repositories.Write;

namespace Homeless.Application.UseCases.Plans;

public sealed class SyncPlanStripeCatalogHandler(
    IPlanReadRepository planReadRepository,
    IPlanWriteRepository planWriteRepository,
    IStripePlanCatalogGateway stripePlanCatalogGateway,
    IUnitOfWork unitOfWork)
    : IRequestHandler<SyncPlanStripeCatalogCommand, Result<PlanResponse>>
{
    public async Task<Result<PlanResponse>> Handle(
        SyncPlanStripeCatalogCommand request,
        CancellationToken cancellationToken)
    {
        var plan = await planReadRepository
            .GetByIdAsync(request.PlanId, cancellationToken)
            .ConfigureAwait(false);

        if (plan is null)
            return Result.NotFound($"Plan '{request.PlanId}' not found.");

        var catalog = await stripePlanCatalogGateway.UpsertPlanCatalogAsync(
            new PlanStripeCatalogRequest(
                plan.Id,
                plan.Name,
                plan.Description,
                plan.Price.AmountInCents / 100m,
                plan.Price.Currency.Code,
                plan.StripePriceId),
            cancellationToken).ConfigureAwait(false);

        plan.SetStripePriceId(catalog.StripePriceId);

        await planWriteRepository.UpdateAsync(plan, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(plan.ToResponse());
    }
}
