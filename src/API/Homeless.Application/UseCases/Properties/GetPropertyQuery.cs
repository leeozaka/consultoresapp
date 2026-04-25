using Ardalis.Result;
using MediatR;
using Homeless.Application.DTOs;

namespace Homeless.Application.UseCases.Properties;

public sealed record GetPropertyQuery(Guid PropertyId) : IRequest<Result<PropertyResponse>>;
