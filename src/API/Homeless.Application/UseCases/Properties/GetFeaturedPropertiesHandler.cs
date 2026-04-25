using Ardalis.Result;
using MediatR;
using Homeless.Application.DTOs;
using Homeless.Application.Interfaces;
using Homeless.Application.Mappers;

namespace Homeless.Application.UseCases.Properties;

public sealed class GetFeaturedPropertiesHandler(
    IPropertyQueryService queryService,
    IStorageService storage)
    : IRequestHandler<GetFeaturedPropertiesQuery, Result<PaginatedResponse<FeaturedPropertyResponse>>>
{
    public async Task<Result<PaginatedResponse<FeaturedPropertyResponse>>> Handle(
        GetFeaturedPropertiesQuery request,
        CancellationToken cancellationToken)
    {
        var (items, totalCount) = await queryService
            .GetFeaturedAsync(request.Page, request.PageSize, cancellationToken)
            .ConfigureAwait(false);

        var responses = items
            .Select(f => f with { Property = f.Property.WithRawImageUrls(storage) })
            .ToList()
            .AsReadOnly();

        var paginated = PaginatedResponse<FeaturedPropertyResponse>.Create(
            responses,
            totalCount,
            request.Page,
            request.PageSize);

        return Result.Success(paginated);
    }
}
