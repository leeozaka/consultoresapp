using Ardalis.Result;
using MediatR;
using Homeless.Application.DTOs;
using Homeless.Application.Interfaces;

namespace Homeless.Application.UseCases.Tenants;

public sealed class GetTenantByIdHandler(ITenantQueryService tenantQueryService)
    : IRequestHandler<GetTenantByIdQuery, Result<TenantResponse>>
{
    public async Task<Result<TenantResponse>> Handle(
        GetTenantByIdQuery request,
        CancellationToken cancellationToken)
    {
        var tenant = await tenantQueryService
            .GetByIdAsync(request.TenantId, cancellationToken)
            .ConfigureAwait(false);

        if (tenant is null)
            return Result.NotFound($"Tenant '{request.TenantId}' was not found.");

        return Result.Success(tenant);
    }
}
