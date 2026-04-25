using Ardalis.Result;
using MediatR;
using Homeless.Application.DTOs;
using Homeless.Application.Interfaces;

namespace Homeless.Application.UseCases.Tenants;

public sealed class GetTenantBySlugHandler(ITenantQueryService tenantQueryService)
    : IRequestHandler<GetTenantBySlugQuery, Result<TenantResponse>>
{
    public async Task<Result<TenantResponse>> Handle(
        GetTenantBySlugQuery request,
        CancellationToken cancellationToken)
    {
        var tenant = await tenantQueryService
            .GetBySlugAsync(request.Slug, cancellationToken)
            .ConfigureAwait(false);

        if (tenant is null)
            return Result.NotFound($"Tenant with slug '{request.Slug}' was not found.");

        return Result.Success(tenant);
    }
}
