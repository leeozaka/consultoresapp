using Ardalis.Result;
using MediatR;
using Homeless.Application.DTOs;
using Homeless.Application.Interfaces;

namespace Homeless.Application.UseCases.Tenants;

public sealed class GetTenantByHostHandler(ITenantQueryService tenantQueryService)
    : IRequestHandler<GetTenantByHostQuery, Result<TenantResponse>>
{
    public async Task<Result<TenantResponse>> Handle(
        GetTenantByHostQuery request,
        CancellationToken cancellationToken)
    {
        var host = TenantHostResolver.NormalizeHost(request.Host);
        if (string.IsNullOrWhiteSpace(host))
            return Result.NotFound("Tenant could not be resolved for the current host.");

        var byCustomDomain = await tenantQueryService
            .GetByCustomDomainAsync(host, cancellationToken)
            .ConfigureAwait(false);

        if (byCustomDomain is not null)
            return Result.Success(byCustomDomain);

        var slug = TenantHostResolver.ResolveSlug(host, request.LandingDomain, request.LandingTenantSlug);
        if (string.IsNullOrWhiteSpace(slug))
            return Result.NotFound("Tenant could not be resolved for the current host.");

        var bySlug = await tenantQueryService
            .GetBySlugAsync(slug, cancellationToken)
            .ConfigureAwait(false);

        if (bySlug is null)
            return Result.NotFound($"Tenant with slug '{slug}' was not found.");

        return Result.Success(bySlug);
    }
}
