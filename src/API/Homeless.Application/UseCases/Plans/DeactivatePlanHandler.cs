using Ardalis.Result;
using MediatR;
using Homeless.Application.Interfaces;
using Homeless.Domain.Interfaces.Repositories.Read;
using Homeless.Domain.Interfaces.Repositories.Write;

namespace Homeless.Application.UseCases.Plans;

public sealed class DeactivatePlanHandler(
    IPlanReadRepository planReadRepository,
    IPlanWriteRepository planWriteRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeactivatePlanCommand, Result>
{
    public async Task<Result> Handle(
        DeactivatePlanCommand request,
        CancellationToken cancellationToken)
    {
        var plan = await planReadRepository
            .GetByIdAsync(request.PlanId, cancellationToken)
            .ConfigureAwait(false);

        if (plan is null)
            return Result.NotFound($"Plan '{request.PlanId}' not found.");

        plan.Deactivate();

        await planWriteRepository.UpdateAsync(plan, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
