using Ardalis.Result;
using MediatR;
using Homeless.Application.Authorization;
using Homeless.Application.DTOs;

namespace Homeless.Application.UseCases.Users;

[RequireRole(Roles.SuperAdmin)]
public sealed record CreateUserCommand(
    string Email,
    string Password,
    string? FirstName,
    string? LastName,
    Guid? TenantId,
    string? Role
) : IRequest<Result<UserResponse>>;
