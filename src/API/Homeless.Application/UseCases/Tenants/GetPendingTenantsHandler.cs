using Ardalis.Result;
using MediatR;
using Homeless.Application.DTOs;
using Homeless.Application.Interfaces;
using Homeless.Domain.Enums;

namespace Homeless.Application.UseCases.Tenants;

public sealed class GetPendingTenantsHandler(ITenantQueryService tenantQueryService)
    : IRequestHandler<GetPendingTenantsQuery, Result<IReadOnlyList<TenantResponse>>>
{
    public async Task<Result<IReadOnlyList<TenantResponse>>> Handle(
        GetPendingTenantsQuery request,
        CancellationToken cancellationToken)
    {
        var tenants = await tenantQueryService
            .GetByStatusAsync(TenantStatus.Pending, cancellationToken)
            .ConfigureAwait(false);

        return Result.Success(tenants);
    }
}
