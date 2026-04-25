using Ardalis.Result;
using MediatR;
using Homeless.Application.DTOs;
using Homeless.Application.Interfaces;
using Homeless.Application.Mappers;
using Homeless.Domain.Interfaces.Repositories.Read;
using Homeless.Domain.Interfaces.Repositories.Write;

namespace Homeless.Application.UseCases.Tenants;

public sealed class AssignTenantPlanHandler(
    ITenantReadRepository tenantReadRepository,
    ITenantWriteRepository tenantWriteRepository,
    IPlanReadRepository planReadRepository,
    ICacheService cacheService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<AssignTenantPlanCommand, Result<TenantResponse>>
{
    public async Task<Result<TenantResponse>> Handle(
        AssignTenantPlanCommand request,
        CancellationToken cancellationToken)
    {
        var tenant = await tenantReadRepository
            .GetByIdAsync(request.TenantId, cancellationToken)
            .ConfigureAwait(false);

        if (tenant is null)
            return Result.NotFound($"Tenant '{request.TenantId}' not found.");

        var plan = await planReadRepository.GetByIdAsync(request.PlanId, cancellationToken).ConfigureAwait(false);
        if (plan is null)
            return Result.NotFound($"Plan '{request.PlanId}' not found.");

        tenant.ChangePlan(plan);

        await tenantWriteRepository.UpdateAsync(tenant, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await cacheService.RemoveAsync(CacheKeys.TenantEntitlements(tenant.Id), cancellationToken).ConfigureAwait(false);
        await cacheService.RemoveAsync(CacheKeys.TenantBySlug(tenant.Slug), cancellationToken).ConfigureAwait(false);
        if (tenant.CustomDomain is not null)
            await cacheService.RemoveAsync(CacheKeys.TenantByDomain(tenant.CustomDomain), cancellationToken).ConfigureAwait(false);

        return Result.Success(tenant.ToResponse());
    }
}
