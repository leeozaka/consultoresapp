using Ardalis.Result;
using MediatR;
using Homeless.Application.Authorization;
using Homeless.Application.DTOs;

namespace Homeless.Application.UseCases.Tenants;

[RequireRole(Roles.TenantAdmin)]
public sealed record UpdateCurrentTenantBrandingCommand(
    UpdateTenantBrandingRequest Request
) : IRequest<Result<TenantResponse>>;
