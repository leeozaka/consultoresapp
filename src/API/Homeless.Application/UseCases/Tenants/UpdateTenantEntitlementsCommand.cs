using Ardalis.Result;
using MediatR;
using Homeless.Application.Authorization;
using Homeless.Application.DTOs;

namespace Homeless.Application.UseCases.Tenants;

[RequireRole(Roles.SuperAdmin)]
public sealed record UpdateTenantEntitlementsCommand(
    Guid TenantId,
    Dictionary<string, object> Entitlements
) : IRequest<Result<TenantResponse>>;
