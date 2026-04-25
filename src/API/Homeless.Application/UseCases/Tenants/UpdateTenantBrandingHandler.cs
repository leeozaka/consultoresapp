using Ardalis.Result;
using MediatR;
using Homeless.Application.Branding;
using Homeless.Application.DTOs;
using Homeless.Application.Interfaces;
using Homeless.Application.Mappers;
using Homeless.Domain.Interfaces.Repositories.Read;
using Homeless.Domain.Interfaces.Repositories.Write;

namespace Homeless.Application.UseCases.Tenants;

/// <summary>
/// Handles branding updates issued by SuperAdmin for any tenant.
/// Intentionally bypasses portal_theme checks — SuperAdmin also controls
/// plan assignment, so they may set rich PortalContent regardless of the
/// tenant's current theme tier.
/// </summary>
public sealed class UpdateTenantBrandingHandler(
    ITenantReadRepository tenantReadRepository,
    ITenantWriteRepository tenantWriteRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateTenantBrandingCommand, Result<TenantResponse>>
{
    public async Task<Result<TenantResponse>> Handle(
        UpdateTenantBrandingCommand request,
        CancellationToken cancellationToken)
    {
        var tenant = await tenantReadRepository
            .GetByIdAsync(request.TenantId, cancellationToken)
            .ConfigureAwait(false);

        if (tenant is null)
            return Result.NotFound($"Tenant '{request.TenantId}' not found.");

        var merged = TenantBrandingMerge.MergeFromAdminRequest(tenant.Branding, request.Request);
        tenant.UpdateBranding(merged);

        await tenantWriteRepository.UpdateAsync(tenant, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(tenant.ToResponse());
    }
}
