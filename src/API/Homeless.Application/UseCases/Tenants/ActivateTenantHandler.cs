using Ardalis.Result;
using MediatR;
using Homeless.Application.DTOs;
using Homeless.Application.Interfaces;
using Homeless.Application.Mappers;
using Homeless.Domain.Interfaces.Repositories.Read;
using Homeless.Domain.Interfaces.Repositories.Write;

namespace Homeless.Application.UseCases.Tenants;

public sealed class ActivateTenantHandler(
    ITenantReadRepository tenantReadRepository,
    ITenantWriteRepository tenantWriteRepository,
    ICacheService cacheService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ActivateTenantCommand, Result<TenantResponse>>
{
    public async Task<Result<TenantResponse>> Handle(
        ActivateTenantCommand request,
        CancellationToken cancellationToken)
    {
        var tenant = await tenantReadRepository
            .GetByIdAsync(request.TenantId, cancellationToken)
            .ConfigureAwait(false);

        if (tenant is null)
            return Result.NotFound($"Tenant '{request.TenantId}' not found.");

        tenant.Activate();

        await tenantWriteRepository.UpdateAsync(tenant, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Invalidate tenant resolution caches
        await cacheService.RemoveAsync(CacheKeys.TenantBySlug(tenant.Slug), cancellationToken).ConfigureAwait(false);
        if (tenant.CustomDomain is not null)
            await cacheService.RemoveAsync(CacheKeys.TenantByDomain(tenant.CustomDomain), cancellationToken).ConfigureAwait(false);

        return Result.Success(tenant.ToResponse());
    }
}
