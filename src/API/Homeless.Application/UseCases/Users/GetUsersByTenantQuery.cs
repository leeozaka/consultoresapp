using Ardalis.Result;
using MediatR;
using Homeless.Application.Authorization;
using Homeless.Application.DTOs;

namespace Homeless.Application.UseCases.Users;

[RequireRole(Roles.SuperAdmin)]
public sealed record GetUsersByTenantQuery(Guid TenantId) : IRequest<Result<IReadOnlyList<UserResponse>>>;
