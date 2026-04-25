using Ardalis.Result;
using MediatR;
using Homeless.Application.Authorization;
using Homeless.Application.DTOs;

namespace Homeless.Application.UseCases.Tenants;

[RequireRole(Roles.SuperAdmin)]
public sealed record CreateTenantCommand(
    string Name,
    string Slug,
    string ContactEmail,
    string? ContactPhone = null,
    string? CustomDomain = null,
    DateTime? NextBillingDate = null
) : IRequest<Result<TenantResponse>>;
