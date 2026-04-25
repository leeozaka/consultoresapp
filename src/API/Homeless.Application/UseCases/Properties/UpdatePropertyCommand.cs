using Ardalis.Result;
using MediatR;
using Homeless.Application.Authorization;
using Homeless.Application.DTOs;

namespace Homeless.Application.UseCases.Properties;

/// <summary>
/// Command to update an existing property listing.
/// Mutable fields are grouped in <see cref="PropertyWriteData"/> to reduce parameter count.
/// </summary>
[RequireRole(Roles.TenantAdmin, Roles.Agent)]
public sealed record UpdatePropertyCommand(
    Guid PropertyId,
    PropertyWriteData Data
) : IRequest<Result<PropertyResponse>>;
