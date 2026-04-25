using Ardalis.Result;
using FluentAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Homeless.Application.DTOs;
using Homeless.Application.Interfaces;
using Homeless.Application.Options;
using Homeless.Application.UseCases.Properties;
using Homeless.Domain.Enums;
using Homeless.Domain.ValueObjects;
using Xunit;

namespace Homeless.UnitTests.Application;

public class SearchPropertiesHandlerTests
{
    private readonly IPropertyQueryService _queryService = Substitute.For<IPropertyQueryService>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly IStorageService _storage = Substitute.For<IStorageService>();
    private readonly SearchPropertiesHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();

    public SearchPropertiesHandlerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _handler = new SearchPropertiesHandler(_queryService, _tenantContext, _storage);
    }

    [Fact]
    public async Task Handle_WithMatchingProperties_ShouldReturnPaginatedResponse()
    {
        var propertyId = Guid.NewGuid();
        IReadOnlyList<PropertyResponse> responses = [BuildResponse(propertyId, _tenantId)];

        _queryService.SearchAsync(_tenantId, Arg.Any<PropertyFilter>(), 1, 20, Arg.Any<CancellationToken>())
            .Returns((responses, 1));

        var query = new SearchPropertiesQuery(new PropertyFilter(null, null, null, null, null, null, null));

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.TotalCount.Should().Be(1);
        result.Value.Page.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WhenRepositoryReturnsEmpty_ShouldReturnEmptyPage()
    {
        IReadOnlyList<PropertyResponse> empty = [];

        _queryService.SearchAsync(_tenantId, Arg.Any<PropertyFilter>(), 1, 20, Arg.Any<CancellationToken>())
            .Returns((empty, 0));

        var query = new SearchPropertiesQuery(new PropertyFilter(null, null, null, null, null, null, null));

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ShouldForwardPageAndPageSizeToQueryService()
    {
        IReadOnlyList<PropertyResponse> empty = [];

        _queryService.SearchAsync(_tenantId, Arg.Any<PropertyFilter>(), 2, 5, Arg.Any<CancellationToken>())
            .Returns((empty, 0));

        var query = new SearchPropertiesQuery(new PropertyFilter(null, null, null, null, null, null, null), Page: 2, PageSize: 5);

        await _handler.Handle(query, CancellationToken.None);

        await _queryService.Received(1)
            .SearchAsync(_tenantId, Arg.Any<PropertyFilter>(), 2, 5, Arg.Any<CancellationToken>());
    }

    private static PropertyResponse BuildResponse(Guid id, Guid tenantId)
    {
        var now = DateTime.UtcNow;
        return new PropertyResponse(id, "Test Property", null, 250_000m, "BRL",
            "São Paulo", "SP", "BR", null, null, 2, 1, null, null,
            PropertyType.Apartment, ListingType.Sale, PropertyStatus.Active,
            null, new List<PropertyImageResponse>().AsReadOnly());
    }
}
