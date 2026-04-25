using Ardalis.Result;
using MediatR;
using Microsoft.Extensions.Options;
using Homeless.Application.DTOs;
using Homeless.Application.Interfaces;
using Homeless.Application.Mappers;
using Homeless.Application.Options;

namespace Homeless.Application.UseCases.Properties;

public sealed class GetPropertyHandler(
    IPropertyQueryService queryService,
    ICacheService cacheService,
    IOptions<CachingOptions> cachingOptions,
    IStorageService storage)
    : IRequestHandler<GetPropertyQuery, Result<PropertyResponse>>
{
    private readonly CachingOptions _caching = cachingOptions.Value;

    public async Task<Result<PropertyResponse>> Handle(
        GetPropertyQuery request,
        CancellationToken cancellationToken)
    {
        var cacheKey = CacheKeys.Property(request.PropertyId);

        var cached = await cacheService
            .GetAsync<PropertyResponse>(cacheKey, cancellationToken)
            .ConfigureAwait(false);

        if (cached is not null)
            return Result.Success(cached);

        var response = await queryService
            .GetByIdAsync(request.PropertyId, cancellationToken)
            .ConfigureAwait(false);

        if (response is null)
            return Result.NotFound($"Property '{request.PropertyId}' not found.");

        var patched = response.WithRawImageUrls(storage);
        await cacheService.SetAsync(cacheKey, patched, TimeSpan.FromSeconds(_caching.PropertyTtlSeconds), cancellationToken).ConfigureAwait(false);

        return Result.Success(patched);
    }
}
