using Homeless.Application.DTOs;
using FluentAssertions;
using Xunit;

namespace Homeless.UnitTests.DTOs;

public class PaginatedResponseTests
{
    [Fact]
    public void Create_ClampsPageAndPageSize_ToAtLeastOne()
    {
        var p = PaginatedResponse<int>.Create([1, 2], totalCount: 2, page: 0, pageSize: 0);

        p.Page.Should().Be(1);
        p.PageSize.Should().Be(1);
        p.TotalPages.Should().Be(2);
    }

    [Fact]
    public void CreateFrom_NormalizesPageAndPageSize_And_SlicesInMemory()
    {
        var all = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        var p = PaginatedResponse<int>.CreateFrom(all, page: 2, pageSize: 3);

        p.Items.Should().Equal(4, 5, 6);
        p.TotalCount.Should().Be(10);
        p.Page.Should().Be(2);
        p.PageSize.Should().Be(3);
        p.TotalPages.Should().Be(4);
    }

    [Fact]
    public void CreateFrom_ClampsPageAndPageSize_ToAtLeastOne()
    {
        var all = new[] { 1, 2 };
        var p = PaginatedResponse<int>.CreateFrom(all, page: 0, pageSize: 0);

        p.Page.Should().Be(1);
        p.PageSize.Should().Be(1);
        p.Items.Should().Equal(1);
    }

    [Fact]
    public void CreateFrom_WhenPagePastEnd_ReturnsEmptyItemsWithCorrectTotals()
    {
        var all = new[] { 1, 2 };
        var p = PaginatedResponse<int>.CreateFrom(all, page: 10, pageSize: 1);

        p.Items.Should().BeEmpty();
        p.TotalCount.Should().Be(2);
        p.TotalPages.Should().Be(2);
    }
}
