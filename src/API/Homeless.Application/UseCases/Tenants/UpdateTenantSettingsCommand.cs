using Ardalis.Result;
using MediatR;
using Homeless.Application.Authorization;
using Homeless.Application.DTOs;

namespace Homeless.Application.UseCases.Tenants;

[RequireRole(Roles.TenantAdmin)]
public sealed record UpdateTenantSettingsCommand(
    string Name,
    string ContactEmail,
    string? ContactPhone = null
) : IRequest<Result<TenantResponse>>;
