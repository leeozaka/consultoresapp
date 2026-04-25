using Ardalis.Result;
using MediatR;
using Homeless.Application.DTOs;
using Homeless.Application.Interfaces;
using Homeless.Application.Mappers;
using Homeless.Domain.Interfaces.Repositories.Read;
using Homeless.Domain.Interfaces.Repositories.Write;

namespace Homeless.Application.UseCases.Tenants;

public sealed class UpdateTenantSettingsHandler(
    ITenantContext tenantContext,
    ITenantReadRepository tenantReadRepository,
    ITenantWriteRepository tenantWriteRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateTenantSettingsCommand, Result<TenantResponse>>
{
    public async Task<Result<TenantResponse>> Handle(
        UpdateTenantSettingsCommand request,
        CancellationToken cancellationToken)
    {
        if (!tenantContext.IsResolved)
            return Result.Forbidden("No tenant context resolved.");

        var tenant = await tenantReadRepository
            .GetByIdAsync(tenantContext.TenantId, cancellationToken)
            .ConfigureAwait(false);

        if (tenant is null)
            return Result.NotFound("Tenant not found.");

        // TenantAdmin can only update Name, ContactEmail, ContactPhone —
        // the slug stays the same so there's no uniqueness conflict.
        tenant.UpdateDetails(request.Name, tenant.Slug, request.ContactEmail, request.ContactPhone);

        await tenantWriteRepository.UpdateAsync(tenant, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(tenant.ToResponse());
    }
}
