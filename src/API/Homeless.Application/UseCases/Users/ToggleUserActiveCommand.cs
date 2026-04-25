using Ardalis.Result;
using MediatR;
using Homeless.Application.Authorization;

namespace Homeless.Application.UseCases.Users;

[RequireRole(Roles.SuperAdmin)]
public sealed record ToggleUserActiveCommand(Guid UserId, bool IsActive) : IRequest<Result>;
