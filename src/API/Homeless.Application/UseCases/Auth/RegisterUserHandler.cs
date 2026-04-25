using Ardalis.Result;
using MediatR;
using Homeless.Application.Interfaces;

namespace Homeless.Application.UseCases.Auth;

public sealed class RegisterUserHandler(IIdentityService identityService)
    : IRequestHandler<RegisterUserCommand, Result<RegisteredUserResponse>>
{
    public async Task<Result<RegisteredUserResponse>> Handle(
        RegisterUserCommand request,
        CancellationToken cancellationToken)
    {
        if (await identityService.EmailExistsAsync(request.Email, cancellationToken).ConfigureAwait(false))
            return Result.Error("An account with this email already exists.");

        var (success, userId, errors) = await identityService
            .CreateUserAsync(request.Email, request.Password, request.FirstName, request.LastName, cancellationToken)
            .ConfigureAwait(false);

        if (!success)
            return Result.Invalid(errors.Select(e => new ValidationError(e)).ToArray());

        return Result.Created(new RegisteredUserResponse(userId, request.Email));
    }
}
