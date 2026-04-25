using Ardalis.Result;
using MediatR;
using Homeless.Application.DTOs;
using Homeless.Application.Interfaces;
using Homeless.Application.Mappers;
using Homeless.Domain.Interfaces.Repositories.Read;
using Homeless.Domain.Interfaces.Repositories.Write;
using Homeless.Domain.ValueObjects;

namespace Homeless.Application.UseCases.Plans;

public sealed class UpdatePlanHandler(
    IPlanReadRepository planReadRepository,
    IPlanWriteRepository planWriteRepository,
    IStripePlanCatalogGateway stripePlanCatalogGateway,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdatePlanCommand, Result<PlanResponse>>
{
    public async Task<Result<PlanResponse>> Handle(
        UpdatePlanCommand request,
        CancellationToken cancellationToken)
    {
        var plan = await planReadRepository
            .GetByIdAsync(request.PlanId, cancellationToken)
            .ConfigureAwait(false);

        if (plan is null)
            return Result.NotFound($"Plan '{request.PlanId}' not found.");

        var newPrice = Money.Create((long)(request.PricePerMonth * 100), plan.Price.Currency);
        var catalog = await stripePlanCatalogGateway.UpsertPlanCatalogAsync(
            new PlanStripeCatalogRequest(
                plan.Id,
                request.Name,
                request.Description,
                newPrice.AmountInCents / 100m,
                newPrice.Currency.Code,
                string.IsNullOrWhiteSpace(request.StripePriceId) ? plan.StripePriceId : request.StripePriceId),
            cancellationToken).ConfigureAwait(false);

        plan.Update(
            request.Name,
            request.Description,
            newPrice,
            request.MaxProperties,
            request.VideoUpload,
            request.AiDescriptions,
            request.CustomDomain,
            request.PremiumAnalytics,
            catalog.StripePriceId,
            portalTheme: request.PortalTheme);

        await planWriteRepository.UpdateAsync(plan, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(plan.ToResponse());
    }
}
