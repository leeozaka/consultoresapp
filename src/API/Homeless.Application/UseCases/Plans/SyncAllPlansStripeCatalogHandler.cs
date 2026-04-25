using Ardalis.Result;
using MediatR;
using Homeless.Application.DTOs;
using Homeless.Application.Interfaces;
using Homeless.Application.Mappers;
using Homeless.Domain.Interfaces.Repositories.Read;
using Homeless.Domain.Interfaces.Repositories.Write;

namespace Homeless.Application.UseCases.Plans;

public sealed class SyncAllPlansStripeCatalogHandler(
    IPlanReadRepository planReadRepository,
    IPlanWriteRepository planWriteRepository,
    IStripePlanCatalogGateway stripePlanCatalogGateway,
    IUnitOfWork unitOfWork)
    : IRequestHandler<SyncAllPlansStripeCatalogCommand, Result<IReadOnlyList<PlanResponse>>>
{
    public async Task<Result<IReadOnlyList<PlanResponse>>> Handle(
        SyncAllPlansStripeCatalogCommand request,
        CancellationToken cancellationToken)
    {
        var plans = await planReadRepository.GetAllPlansAsync(cancellationToken).ConfigureAwait(false);
        var responses = new List<PlanResponse>(plans.Count);

        foreach (var plan in plans)
        {
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
            responses.Add(plan.ToResponse());
        }

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success<IReadOnlyList<PlanResponse>>(responses);
    }
}
