using Ardalis.Result;
using MediatR;
using Homeless.Application.Authorization;
using Homeless.Application.DTOs;

namespace Homeless.Application.UseCases.Properties;

/// <summary>
/// Command to create a new property listing.
/// Mutable fields are grouped in <see cref="PropertyWriteData"/> to reduce parameter count.
/// </summary>
[RequireRole(Roles.TenantAdmin, Roles.Agent)]
public sealed record CreatePropertyCommand(
    PropertyWriteData Data,
    string Country = "BR"
) : IRequest<Result<PropertyResponse>>;
