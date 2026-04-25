using Ardalis.Result;
using MediatR;
using Homeless.Application.DTOs;
using Homeless.Application.Interfaces;
using Homeless.Application.Mappers;
using Homeless.Domain.Interfaces.Repositories.Read;
using Homeless.Domain.Interfaces.Repositories.Write;

namespace Homeless.Application.UseCases.Tenants;

public sealed class SetTenantCustomDomainHandler(
    ITenantReadRepository tenantReadRepository,
    ITenantWriteRepository tenantWriteRepository,
    ICacheService cacheService,
    IUnitOfWork unitOfWork,
    ISiteBuildChannel siteBuildChannel,
    ITenantOriginService tenantOriginService,
    IOidcClientRedirectService oidcClientRedirectService)
    : IRequestHandler<SetTenantCustomDomainCommand, Result<TenantResponse>>
{
    public async Task<Result<TenantResponse>> Handle(
        SetTenantCustomDomainCommand request,
        CancellationToken cancellationToken)
    {
        var tenant = await tenantReadRepository
            .GetByIdAsync(request.TenantId, cancellationToken)
            .ConfigureAwait(false);

        if (tenant is null)
            return Result.NotFound($"Tenant '{request.TenantId}' not found.");

        var oldDomain = tenant.CustomDomain;
        var normalizedDomain = NormalizeDomain(request.CustomDomain);

        if (!string.IsNullOrWhiteSpace(normalizedDomain) && !tenant.HasEntitlement("custom_domain"))
            return Result.Forbidden("Tenant subscription does not allow custom domains.");

        if (!string.IsNullOrWhiteSpace(normalizedDomain) &&
            !string.Equals(oldDomain, normalizedDomain, StringComparison.OrdinalIgnoreCase))
        {
            var exists = await tenantReadRepository
                .ExistsByCustomDomainAsync(normalizedDomain, cancellationToken)
                .ConfigureAwait(false);

            if (exists)
                return Result.Conflict($"Custom domain '{normalizedDomain}' is already in use.");
        }

        tenant.SetCustomDomain(normalizedDomain);

        await tenantWriteRepository.UpdateAsync(tenant, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await cacheService.RemoveAsync(CacheKeys.TenantBySlug(tenant.Slug), cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(oldDomain))
            await cacheService.RemoveAsync(CacheKeys.TenantByDomain(oldDomain), cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(normalizedDomain))
            await cacheService.RemoveAsync(CacheKeys.TenantByDomain(normalizedDomain), cancellationToken).ConfigureAwait(false);

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

    private static string? NormalizeDomain(string? domain) =>
        string.IsNullOrWhiteSpace(domain)
            ? null
            : domain.Trim().ToLowerInvariant();
}
