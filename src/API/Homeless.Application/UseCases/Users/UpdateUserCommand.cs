using Ardalis.Result;
using MediatR;
using Homeless.Application.Authorization;
using Homeless.Application.DTOs;

namespace Homeless.Application.UseCases.Users;

[RequireRole(Roles.SuperAdmin)]
public sealed record UpdateUserCommand(
    Guid UserId,
    string? FirstName,
    string? LastName,
    string? Email
) : IRequest<Result<UserResponse>>;
