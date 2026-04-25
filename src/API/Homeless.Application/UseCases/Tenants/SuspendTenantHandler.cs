using Ardalis.Result;
using MediatR;
using Homeless.Application.DTOs;
using Homeless.Application.Mappers;
using Homeless.Domain.Interfaces.Repositories.Read;
using Homeless.Domain.Interfaces.Repositories.Write;
using Homeless.Application.Interfaces;

namespace Homeless.Application.UseCases.Tenants;

public sealed class SuspendTenantHandler(
    ITenantReadRepository tenantReadRepository,
    ITenantWriteRepository tenantWriteRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<SuspendTenantCommand, Result<TenantResponse>>
{
    public async Task<Result<TenantResponse>> Handle(
        SuspendTenantCommand request,
        CancellationToken cancellationToken)
    {
        var tenant = await tenantReadRepository
            .GetByIdAsync(request.TenantId, cancellationToken)
            .ConfigureAwait(false);

        if (tenant is null)
            return Result.NotFound($"Tenant '{request.TenantId}' not found.");

        tenant.Suspend(request.Reason ?? "Suspended by SuperAdmin");

        await tenantWriteRepository.UpdateAsync(tenant, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(tenant.ToResponse());
    }
}
