using Ardalis.Result;
using MediatR;
using Microsoft.Extensions.Options;
using Homeless.Application.DTOs;
using Homeless.Application.Interfaces;
using Homeless.Application.Options;

namespace Homeless.Application.UseCases.Properties;

public sealed class GetLocationSuggestionsHandler(
    IPropertyQueryService queryService,
    ITenantContext tenantContext,
    ICacheService cacheService,
    IOptions<CachingOptions> cachingOptions)
    : IRequestHandler<GetLocationSuggestionsQuery, Result<IReadOnlyList<LocationSuggestionResponse>>>
{
    private readonly CachingOptions _caching = cachingOptions.Value;

    public async Task<Result<IReadOnlyList<LocationSuggestionResponse>>> Handle(
        GetLocationSuggestionsQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = CacheKeys.LocationSuggestions(tenantContext.TenantId, request.Q);

        var suggestions = await cacheService.GetOrCreateAsync(
            cacheKey,
            ct => queryService.GetLocationSuggestionsAsync(tenantContext.TenantId, request.Q, ct),
            TimeSpan.FromSeconds(_caching.LocationSuggestionsTtlSeconds),
            cancellationToken);

        return Result.Success(suggestions);
    }
}
