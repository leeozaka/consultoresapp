using Ardalis.Result;
using MediatR;
using Homeless.Application.DTOs;
using Homeless.Application.Interfaces;

namespace Homeless.Application.UseCases.Plans;

public sealed class GetPlansHandler(IPlanQueryService planQueryService)
    : IRequestHandler<GetPlansQuery, Result<IReadOnlyList<PlanResponse>>>
{
    public async Task<Result<IReadOnlyList<PlanResponse>>> Handle(
        GetPlansQuery request,
        CancellationToken cancellationToken)
    {
        var plans = request.ActiveOnly
            ? await planQueryService.GetActivePlansAsync(cancellationToken).ConfigureAwait(false)
            : await planQueryService.GetAllPlansAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(plans);
    }
}
