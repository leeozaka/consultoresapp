using Ardalis.Result;
using FluentAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Homeless.Application.DTOs;
using Homeless.Application.Interfaces;
using Homeless.Application.Options;
using Homeless.Application.UseCases.Properties;
using Homeless.Domain.Enums;
using Xunit;

namespace Homeless.UnitTests.Application;

public class GetPropertyHandlerTests
{
    private readonly IPropertyQueryService _queryService = Substitute.For<IPropertyQueryService>();
    private readonly ICacheService _cache = Substitute.For<ICacheService>();
    private readonly IStorageService _storage = Substitute.For<IStorageService>();
    private readonly GetPropertyHandler _handler;
    private readonly Guid _propertyId = Guid.NewGuid();
    private readonly Guid _tenantId = Guid.NewGuid();

    public GetPropertyHandlerTests()
    {
        var options = Options.Create(new CachingOptions { PropertyTtlSeconds = 300 });
        _handler = new GetPropertyHandler(_queryService, _cache, options, _storage);
    }

    [Fact]
    public async Task Handle_WhenCacheHit_ShouldReturnCachedResponseWithoutCallingQueryService()
    {
        var cached = BuildResponse(_propertyId, _tenantId);
        _cache.GetAsync<PropertyResponse>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(cached);

        var result = await _handler.Handle(new GetPropertyQuery(_propertyId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _queryService.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCacheMissAndPropertyFound_ShouldReturnResponseAndCacheIt()
    {
        var response = BuildResponse(_propertyId, _tenantId);
        _cache.GetAsync<PropertyResponse>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((PropertyResponse?)null);
        _queryService.GetByIdAsync(_propertyId, Arg.Any<CancellationToken>())
            .Returns(response);

        var result = await _handler.Handle(new GetPropertyQuery(_propertyId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _cache.Received(1).SetAsync(
            Arg.Any<string>(),
            Arg.Any<PropertyResponse>(),
            Arg.Any<TimeSpan?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenPropertyNotFound_ShouldReturnNotFound()
    {
        _cache.GetAsync<PropertyResponse>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((PropertyResponse?)null);
        _queryService.GetByIdAsync(_propertyId, Arg.Any<CancellationToken>())
            .Returns((PropertyResponse?)null);

        var result = await _handler.Handle(new GetPropertyQuery(_propertyId), CancellationToken.None);

        result.IsNotFound().Should().BeTrue();
        await _cache.DidNotReceive().SetAsync(
            Arg.Any<string>(),
            Arg.Any<PropertyResponse>(),
            Arg.Any<TimeSpan?>(),
            Arg.Any<CancellationToken>());
    }

    private static PropertyResponse BuildResponse(Guid id, Guid tenantId)
    {
        var now = DateTime.UtcNow;
        return new PropertyResponse(id, "Test Property", null, 500_000m, "BRL",
            "Curitiba", "PR", "BR", "80010-000", null, 3, 2, 1, 120m,
            PropertyType.Apartment, ListingType.Sale, PropertyStatus.Active,
            null, new List<PropertyImageResponse>().AsReadOnly());
    }
}
