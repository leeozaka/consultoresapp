using Ardalis.Result;
using MediatR;
using Homeless.Application.Authorization;
using Homeless.Application.DTOs;

namespace Homeless.Application.UseCases.Properties;

[RequireRole(Roles.TenantAdmin, Roles.Agent)]
public sealed record PublishPropertyCommand(Guid PropertyId) : IRequest<Result<PropertyResponse>>;
