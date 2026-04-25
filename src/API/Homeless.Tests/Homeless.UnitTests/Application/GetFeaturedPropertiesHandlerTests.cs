using FluentAssertions;
using NSubstitute;
using Homeless.Application.DTOs;
using Homeless.Application.Interfaces;
using Homeless.Application.UseCases.Properties;
using Homeless.Domain.Enums;
using Xunit;

namespace Homeless.UnitTests.Application;

public class GetFeaturedPropertiesHandlerTests
{
    private readonly IPropertyQueryService _queryService = Substitute.For<IPropertyQueryService>();
    private readonly IStorageService _storage = Substitute.For<IStorageService>();
    private readonly GetFeaturedPropertiesHandler _handler;

    public GetFeaturedPropertiesHandlerTests()
    {
        _handler = new GetFeaturedPropertiesHandler(_queryService, _storage);
    }

    [Fact]
    public async Task Handle_WithFeaturedProperties_ShouldReturnPaginatedResponse()
    {
        var tenantId = Guid.NewGuid();
        var propertyId = Guid.NewGuid();
        IReadOnlyList<FeaturedPropertyResponse> responses = [BuildFeaturedResponse(propertyId)];

        _queryService.GetFeaturedAsync(1, 10, Arg.Any<CancellationToken>())
            .Returns((responses, 1));

        var query = new GetFeaturedPropertiesQuery(1, 10);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WhenNoFeaturedProperties_ShouldReturnEmptyPage()
    {
        IReadOnlyList<FeaturedPropertyResponse> empty = [];

        _queryService.GetFeaturedAsync(1, 10, Arg.Any<CancellationToken>())
            .Returns((empty, 0));

        var query = new GetFeaturedPropertiesQuery(1, 10);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ShouldForwardPageAndPageSizeToQueryService()
    {
        IReadOnlyList<FeaturedPropertyResponse> empty = [];

        _queryService.GetFeaturedAsync(3, 6, Arg.Any<CancellationToken>())
            .Returns((empty, 0));

        var query = new GetFeaturedPropertiesQuery(3, 6);
        await _handler.Handle(query, CancellationToken.None);

        await _queryService.Received(1).GetFeaturedAsync(3, 6, Arg.Any<CancellationToken>());
    }

    private static FeaturedPropertyResponse BuildFeaturedResponse(Guid id)
    {
        var prop = new PropertyResponse(id, "Featured Property", null, 800_000m, "BRL",
            "Rio de Janeiro", "RJ", "BR", null, null, 3, 2, null, null,
            PropertyType.House, ListingType.Sale, PropertyStatus.Active,
            null, new List<PropertyImageResponse>().AsReadOnly());
        return new FeaturedPropertyResponse(prop, "demo", "Imobiliária Demo", null);
    }
}
