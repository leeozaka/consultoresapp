using Ardalis.Result;
using MediatR;
using Homeless.Application.Interfaces;

namespace Homeless.Application.UseCases.Users;

public sealed class ToggleUserActiveHandler(IUserCommandService userCommandService)
    : IRequestHandler<ToggleUserActiveCommand, Result>
{
    public async Task<Result> Handle(
        ToggleUserActiveCommand request,
        CancellationToken cancellationToken)
    {
        var success = await userCommandService
            .SetActiveAsync(request.UserId, request.IsActive, cancellationToken)
            .ConfigureAwait(false);

        return success
            ? Result.Success()
            : Result.NotFound($"User '{request.UserId}' not found.");
    }
}
