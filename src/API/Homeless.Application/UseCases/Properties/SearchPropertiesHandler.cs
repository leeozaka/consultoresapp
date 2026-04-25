using Ardalis.Result;
using MediatR;
using Homeless.Application.DTOs;
using Homeless.Application.Interfaces;
using Homeless.Application.Mappers;

namespace Homeless.Application.UseCases.Properties;

public sealed class SearchPropertiesHandler(
    IPropertyQueryService queryService,
    ITenantContext tenantContext,
    IStorageService storage)
    : IRequestHandler<SearchPropertiesQuery, Result<PaginatedResponse<PropertyResponse>>>
{
    public async Task<Result<PaginatedResponse<PropertyResponse>>> Handle(
        SearchPropertiesQuery request,
        CancellationToken cancellationToken)
    {
        var (items, totalCount) = await queryService.SearchAsync(
            tenantId: tenantContext.TenantId,
            filter: request.Filter,
            page: request.Page,
            pageSize: request.PageSize,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        // Projection already built the DTOs in the DB; only patch storage URLs for
        // images that haven't been post-processed yet.
        var responses = items.Select(r => r.WithRawImageUrls(storage)).ToList().AsReadOnly();
        var paginated = PaginatedResponse<PropertyResponse>.Create(responses, totalCount, request.Page, request.PageSize);

        return Result.Success(paginated);
    }
}
