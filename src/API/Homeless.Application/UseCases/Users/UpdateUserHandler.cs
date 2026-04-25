using Ardalis.Result;
using MediatR;
using Homeless.Application.DTOs;
using Homeless.Application.Interfaces;

namespace Homeless.Application.UseCases.Users;

public sealed class UpdateUserHandler(
    IIdentityService identityService,
    IUserQueryService userQueryService)
    : IRequestHandler<UpdateUserCommand, Result<UserResponse>>
{
    public async Task<Result<UserResponse>> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        if (!await identityService.UserExistsAsync(request.UserId, cancellationToken).ConfigureAwait(false))
            return Result.NotFound($"User '{request.UserId}' not found.");

        var (success, errors) = await identityService
            .UpdateUserAsync(request.UserId, request.FirstName, request.LastName, request.Email, cancellationToken)
            .ConfigureAwait(false);

        if (!success)
            return Result.Error(string.Join("; ", errors));

        var user = await userQueryService.GetByIdAsync(request.UserId, cancellationToken).ConfigureAwait(false);
        return Result.Success(user!);
    }
}
