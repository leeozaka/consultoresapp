using Ardalis.Result;
using MediatR;
using Homeless.Application.DTOs;
using Homeless.Application.Interfaces;

namespace Homeless.Application.UseCases.Users;

public sealed class GetOrphanedUsersHandler(IUserQueryService userQueryService)
    : IRequestHandler<GetOrphanedUsersQuery, Result<IReadOnlyList<UserResponse>>>
{
    public async Task<Result<IReadOnlyList<UserResponse>>> Handle(
        GetOrphanedUsersQuery request,
        CancellationToken cancellationToken)
    {
        var users = await userQueryService.GetOrphanedAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success(users);
    }
}
