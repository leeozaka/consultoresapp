using Ardalis.Result;
using MediatR;
using Homeless.Application.DTOs;
using Homeless.Application.Interfaces;

namespace Homeless.Application.UseCases.Users;

public sealed class CreateUserHandler(
    IIdentityService identityService,
    IUserQueryService userQueryService)
    : IRequestHandler<CreateUserCommand, Result<UserResponse>>
{
    public async Task<Result<UserResponse>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        if (await identityService.EmailExistsAsync(request.Email, cancellationToken).ConfigureAwait(false))
            return Result.Invalid(new ValidationError("Email is already in use."));

        var (success, userId, errors) = await identityService
            .CreateUserAsync(request.Email, request.Password, request.FirstName, request.LastName, cancellationToken)
            .ConfigureAwait(false);

        if (!success)
            return Result.Error(string.Join("; ", errors));

        if (request.TenantId.HasValue && request.Role is not null)
        {
            if (!await identityService.TenantExistsAsync(request.TenantId.Value, cancellationToken).ConfigureAwait(false))
                return Result.NotFound("Tenant not found.");

            var (assignSuccess, assignErrors) = await identityService
                .AssignTenantAndRoleAsync(userId, request.TenantId.Value, request.Role, activate: true, cancellationToken)
                .ConfigureAwait(false);

            if (!assignSuccess)
                return Result.Error(string.Join("; ", assignErrors));
        }

        var user = await userQueryService.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        return Result.Success(user!);
    }
}
