using Ardalis.Result;
using MediatR;
using Homeless.Application.DTOs;
using Homeless.Application.Interfaces;
using Homeless.Application.Mappers;
using Homeless.Domain.Entities;
using Homeless.Domain.Interfaces.Repositories.Write;
using Homeless.Domain.ValueObjects;

namespace Homeless.Application.UseCases.Plans;

public sealed class CreatePlanHandler(
    IPlanWriteRepository planWriteRepository,
    IStripePlanCatalogGateway stripePlanCatalogGateway,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreatePlanCommand, Result<PlanResponse>>
{
    public async Task<Result<PlanResponse>> Handle(
        CreatePlanCommand request,
        CancellationToken cancellationToken)
    {
        var price = Money.Create((long)(request.PricePerMonth * 100), Currency.BRL);
        var plan = Plan.Create(
            request.Name,
            request.Description,
            price,
            request.MaxProperties,
            request.VideoUpload,
            request.AiDescriptions,
            request.CustomDomain,
            request.PremiumAnalytics,
            stripePriceId: request.StripePriceId,
            portalTheme: request.PortalTheme);

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

        await planWriteRepository.AddAsync(plan, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Created(plan.ToResponse());
    }
}
