using Ardalis.Result;
using MediatR;
using Homeless.Application.DTOs;
using Homeless.Application.Interfaces;

namespace Homeless.Application.UseCases.Users;

public sealed class GetUserByIdHandler(IUserQueryService userQueryService)
    : IRequestHandler<GetUserByIdQuery, Result<UserResponse>>
{
    public async Task<Result<UserResponse>> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await userQueryService.GetByIdAsync(request.UserId, cancellationToken).ConfigureAwait(false);

        return user is null
            ? Result.NotFound($"User '{request.UserId}' not found.")
            : Result.Success(user);
    }
}
