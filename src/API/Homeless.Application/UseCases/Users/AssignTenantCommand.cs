using Ardalis.Result;
using MediatR;
using Homeless.Application.Authorization;

namespace Homeless.Application.UseCases.Users;

[RequireRole(Roles.SuperAdmin)]
public sealed record AssignTenantCommand(
    Guid UserId,
    Guid TenantId,
    string Role
) : IRequest<Result>;
