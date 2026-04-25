using Ardalis.Result;
using MediatR;
using Homeless.Application.DTOs;

namespace Homeless.Application.UseCases.Properties;

public sealed record GetFeaturedPropertiesQuery(
    int Page,
    int PageSize) : IRequest<Result<PaginatedResponse<FeaturedPropertyResponse>>>;
