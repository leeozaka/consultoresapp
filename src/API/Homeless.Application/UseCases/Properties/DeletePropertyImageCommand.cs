using Ardalis.Result;
using MediatR;
using Homeless.Application.Authorization;
using Homeless.Application.DTOs;

namespace Homeless.Application.UseCases.Properties;

[RequireRole(Roles.TenantAdmin, Roles.Agent)]
public sealed record DeletePropertyImageCommand(
    Guid PropertyId,
    string ImageKey
) : IRequest<Result<PropertyResponse>>;
