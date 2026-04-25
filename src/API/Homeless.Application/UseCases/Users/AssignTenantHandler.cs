using Ardalis.Result;
using MediatR;
using Homeless.Application.Authorization;
using Homeless.Application.Interfaces;

namespace Homeless.Application.UseCases.Users;

public sealed class AssignTenantHandler(IIdentityService identityService)
    : IRequestHandler<AssignTenantCommand, Result>
{
    public async Task<Result> Handle(AssignTenantCommand request, CancellationToken cancellationToken)
    {
        if (request.Role is not (Roles.TenantAdmin or Roles.Agent))
            return Result.Invalid(new ValidationError(
                $"Role must be '{Roles.TenantAdmin}' or '{Roles.Agent}'."));

        if (!await identityService.UserExistsAsync(request.UserId, cancellationToken).ConfigureAwait(false))
            return Result.NotFound("User not found.");

        if (!await identityService.TenantExistsAsync(request.TenantId, cancellationToken).ConfigureAwait(false))
            return Result.NotFound("Tenant not found.");

        var (success, errors) = await identityService
            .AssignTenantAndRoleAsync(request.UserId, request.TenantId, request.Role, activate: true, cancellationToken)
            .ConfigureAwait(false);

        return success
            ? Result.Success()
            : Result.Error(string.Join("; ", errors));
    }
}
