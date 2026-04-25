using Ardalis.Result;
using MediatR;
using Homeless.Application.DTOs;
using Homeless.Domain.ValueObjects;

namespace Homeless.Application.UseCases.Properties;

/// <summary>
/// Query to search and paginate property listings.
/// Filter criteria are encapsulated in <see cref="PropertyFilter"/> to avoid a long parameter list.
/// </summary>
public sealed record SearchPropertiesQuery(
    PropertyFilter Filter,
    int Page = 1,
    int PageSize = 20
) : IRequest<Result<PaginatedResponse<PropertyResponse>>>;
