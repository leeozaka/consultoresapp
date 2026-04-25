using Ardalis.Result;
using MediatR;
using Homeless.Application.DTOs;
using Homeless.Application.Interfaces;

namespace Homeless.Application.UseCases.Users;

public sealed class GetUsersByTenantHandler(IUserQueryService userQueryService)
    : IRequestHandler<GetUsersByTenantQuery, Result<IReadOnlyList<UserResponse>>>
{
    public async Task<Result<IReadOnlyList<UserResponse>>> Handle(
        GetUsersByTenantQuery request,
        CancellationToken cancellationToken)
    {
        var users = await userQueryService.GetByTenantIdAsync(request.TenantId, cancellationToken).ConfigureAwait(false);
        return Result.Success(users);
    }
}
