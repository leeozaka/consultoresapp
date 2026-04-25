using Ardalis.Result;
using MediatR;
using Homeless.Application.Authorization;
using Homeless.Application.DTOs;

namespace Homeless.Application.UseCases.Tenants;

[RequireRole(Roles.SuperAdmin)]
public sealed record GetAllTenantsQuery(
    int PageSize = 50,
    string? After = null
) : IRequest<Result<CursorPaginatedResponse<TenantResponse>>>;
