using Ardalis.Result;
using MediatR;
using Homeless.Application.DTOs;
using Homeless.Application.Interfaces;
using Homeless.Application.Mappers;
using Homeless.Domain.Interfaces.Repositories.Read;
using Homeless.Domain.Interfaces.Repositories.Write;

namespace Homeless.Application.UseCases.Tenants;

public sealed class SetTenantFrontendOriginHandler(
    ITenantReadRepository tenantReadRepository,
    ITenantWriteRepository tenantWriteRepository,
    ICacheService cacheService,
    IUnitOfWork unitOfWork,
    ISiteBuildChannel siteBuildChannel,
    ITenantOriginService tenantOriginService,
    IOidcClientRedirectService oidcClientRedirectService)
    : IRequestHandler<SetTenantFrontendOriginCommand, Result<TenantResponse>>
{
    public async Task<Result<TenantResponse>> Handle(
        SetTenantFrontendOriginCommand request,
        CancellationToken cancellationToken)
    {
        var tenant = await tenantReadRepository
            .GetByIdAsync(request.TenantId, cancellationToken)
            .ConfigureAwait(false);

        if (tenant is null)
            return Result.NotFound($"Tenant '{request.TenantId}' not found.");

        var normalizedOrigin = NormalizeOrigin(request.FrontendOrigin);

        if (!string.IsNullOrWhiteSpace(normalizedOrigin) && !tenant.HasEntitlement("custom_frontend"))
            return Result.Forbidden("Tenant subscription does not allow custom frontend origins.");

        tenant.SetFrontendOrigin(normalizedOrigin);

        await tenantWriteRepository.UpdateAsync(tenant, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await cacheService.RemoveAsync(CacheKeys.TenantBySlug(tenant.Slug), cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(tenant.CustomDomain))
            await cacheService.RemoveAsync(CacheKeys.TenantByDomain(tenant.CustomDomain), cancellationToken).ConfigureAwait(false);

        await tenantOriginService.RefreshAsync(cancellationToken).ConfigureAwait(false);
        await oidcClientRedirectService.SyncAsync(cancellationToken).ConfigureAwait(false);

        await siteBuildChannel.WriteAsync(
            new SiteBuildMessage(
                tenant.Id,
                tenant.Slug,
                tenant.CustomDomain,
                tenant.FrontendOrigin),
            cancellationToken).ConfigureAwait(false);

        return Result.Success(tenant.ToResponse());
    }

    private static string? NormalizeOrigin(string? origin)
    {
        if (string.IsNullOrWhiteSpace(origin))
            return null;

        var normalized = origin.Trim();
        return Uri.TryCreate(normalized, UriKind.Absolute, out var parsed)
            ? parsed.GetLeftPart(UriPartial.Authority).ToLowerInvariant()
            : null;
    }
}
