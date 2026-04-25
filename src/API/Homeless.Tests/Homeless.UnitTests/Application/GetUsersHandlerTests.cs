using FluentAssertions;
using NSubstitute;
using Homeless.Application.DTOs;
using Homeless.Application.Interfaces;
using Homeless.Application.UseCases.Users;
using Xunit;

namespace Homeless.UnitTests.Application;

public class GetUsersHandlerTests
{
    private readonly IUserQueryService _queryService = Substitute.For<IUserQueryService>();
    private readonly GetUsersHandler _handler;

    public GetUsersHandlerTests()
    {
        _handler = new GetUsersHandler(_queryService);
    }

    [Fact]
    public async Task Handle_WhenUsersExist_ReturnsAllUsers()
    {
        IReadOnlyList<UserResponse> users =
        [
            BuildUser(Guid.NewGuid(), "alice@example.com", tenantId: Guid.NewGuid()),
            BuildUser(Guid.NewGuid(), "bob@example.com",   tenantId: null),
        ];
        _queryService.GetAllAsync(Arg.Any<CancellationToken>()).Returns(users);

        var result = await _handler.Handle(new GetUsersQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_WhenNoUsersExist_ReturnsEmptyList()
    {
        _queryService.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(Array.Empty<UserResponse>());

        var result = await _handler.Handle(new GetUsersQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_DelegatesToQueryService()
    {
        _queryService.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(Array.Empty<UserResponse>());

        await _handler.Handle(new GetUsersQuery(), CancellationToken.None);

        await _queryService.Received(1).GetAllAsync(Arg.Any<CancellationToken>());
    }

    private static UserResponse BuildUser(Guid id, string email, Guid? tenantId) =>
        new(id, email, "First", "Last", tenantId, null,
            new List<string>().AsReadOnly(), true, DateTime.UtcNow);
}

public class GetOrphanedUsersHandlerTests
{
    private readonly IUserQueryService _queryService = Substitute.For<IUserQueryService>();
    private readonly GetOrphanedUsersHandler _handler;

    public GetOrphanedUsersHandlerTests()
    {
        _handler = new GetOrphanedUsersHandler(_queryService);
    }

    [Fact]
    public async Task Handle_ReturnsOnlyOrphanedUsers()
    {
        IReadOnlyList<UserResponse> orphans =
        [
            BuildUser(Guid.NewGuid(), "orphan@example.com"),
        ];
        _queryService.GetOrphanedAsync(Arg.Any<CancellationToken>()).Returns(orphans);

        var result = await _handler.Handle(new GetOrphanedUsersQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value.Single().TenantId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_DelegatesToQueryService()
    {
        _queryService.GetOrphanedAsync(Arg.Any<CancellationToken>())
            .Returns(Array.Empty<UserResponse>());

        await _handler.Handle(new GetOrphanedUsersQuery(), CancellationToken.None);

        await _queryService.Received(1).GetOrphanedAsync(Arg.Any<CancellationToken>());
    }

    private static UserResponse BuildUser(Guid id, string email) =>
        new(id, email, "First", "Last", null, null,
            new List<string>().AsReadOnly(), true, DateTime.UtcNow);
}
