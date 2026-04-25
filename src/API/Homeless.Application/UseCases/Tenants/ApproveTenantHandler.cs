using Ardalis.Result;
using MediatR;
using Homeless.Application.DTOs;
using Homeless.Application.Interfaces;
using Homeless.Application.Mappers;
using Homeless.Application.UseCases.Onboarding;
using Homeless.Domain.Interfaces.Repositories.Read;
using Homeless.Domain.Interfaces.Repositories.Write;

namespace Homeless.Application.UseCases.Tenants;

public sealed class ApproveTenantHandler(
    ITenantReadRepository tenantReadRepository,
    ITenantWriteRepository tenantWriteRepository,
    ICacheService cacheService,
    IUnitOfWork unitOfWork,
    ISiteBuildChannel siteBuildChannel,
    IIdentityService identityService,
    IOnboardingStatusStream onboardingStatusStream)
    : IRequestHandler<ApproveTenantCommand, Result<TenantResponse>>
{
    public async Task<Result<TenantResponse>> Handle(
        ApproveTenantCommand request,
        CancellationToken cancellationToken)
    {
        var tenant = await tenantReadRepository
            .GetByIdAsync(request.TenantId, cancellationToken)
            .ConfigureAwait(false);

        if (tenant is null)
            return Result.NotFound($"Tenant '{request.TenantId}' not found.");

        tenant.Activate();

        await tenantWriteRepository.UpdateAsync(tenant, cancellationToken).ConfigureAwait(false);

        // Activate all users bound to this tenant (set IsActive = true)
        await identityService
            .ActivateUsersByTenantAsync(tenant.Id, cancellationToken)
            .ConfigureAwait(false);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await cacheService.RemoveAsync(CacheKeys.TenantBySlug(tenant.Slug), cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(tenant.CustomDomain))
            await cacheService.RemoveAsync(CacheKeys.TenantByDomain(tenant.CustomDomain), cancellationToken).ConfigureAwait(false);

        await siteBuildChannel.WriteAsync(
            new SiteBuildMessage(
                tenant.Id,
                tenant.Slug,
                tenant.CustomDomain,
                tenant.FrontendOrigin),
            cancellationToken).ConfigureAwait(false);

        // Notify onboarding status stream so the status SSE page updates
        await onboardingStatusStream.PublishAsync(new OnboardingStatusEvent(
            TenantId: tenant.Id,
            Status: "provisioning",
            Message: "Your agency has been approved. Setting up your portal…"),
            cancellationToken).ConfigureAwait(false);

        return Result.Success(tenant.ToResponse());
    }
}
