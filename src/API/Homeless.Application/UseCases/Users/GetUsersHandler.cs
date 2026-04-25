using Ardalis.Result;
using MediatR;
using Homeless.Application.DTOs;
using Homeless.Application.Interfaces;

namespace Homeless.Application.UseCases.Users;

public sealed class GetUsersHandler(IUserQueryService userQueryService)
    : IRequestHandler<GetUsersQuery, Result<IReadOnlyList<UserResponse>>>
{
    public async Task<Result<IReadOnlyList<UserResponse>>> Handle(
        GetUsersQuery request,
        CancellationToken cancellationToken)
    {
        var users = await userQueryService.GetAllAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success(users);
    }
}
