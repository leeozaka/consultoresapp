using Homeless.Application.DTOs;

namespace Homeless.Application.Interfaces;

public interface IStripePlanCatalogGateway
{
    Task<PlanStripeCatalogResult> UpsertPlanCatalogAsync(
        PlanStripeCatalogRequest request,
        CancellationToken cancellationToken = default);
}
