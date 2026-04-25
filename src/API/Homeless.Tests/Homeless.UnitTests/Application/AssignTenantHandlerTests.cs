using Ardalis.Result;
using FluentAssertions;
using NSubstitute;
using Homeless.Application.Authorization;
using Homeless.Application.Interfaces;
using Homeless.Application.UseCases.Users;
using Xunit;

namespace Homeless.UnitTests.Application;

public class AssignTenantHandlerTests
{
    private readonly IIdentityService _identity = Substitute.For<IIdentityService>();
    private readonly AssignTenantHandler _handler;

    private readonly Guid _userId   = Guid.NewGuid();
    private readonly Guid _tenantId = Guid.NewGuid();

    public AssignTenantHandlerTests()
    {
        _handler = new AssignTenantHandler(_identity);
    }

    [Theory]
    [InlineData(Roles.TenantAdmin)]
    [InlineData(Roles.Agent)]
    public async Task Handle_ValidRoleAndExistingEntities_ReturnsSuccess(string role)
    {
        _identity.UserExistsAsync(_userId, Arg.Any<CancellationToken>()).Returns(true);
        _identity.TenantExistsAsync(_tenantId, Arg.Any<CancellationToken>()).Returns(true);
        _identity.AssignTenantAndRoleAsync(_userId, _tenantId, role, Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns((true, Array.Empty<string>()));

        var cmd    = new AssignTenantCommand(_userId, _tenantId, role);
        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_InvalidRole_ReturnsBadRequest()
    {
        var cmd    = new AssignTenantCommand(_userId, _tenantId, "UnknownRole");
        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.IsInvalid().Should().BeTrue();
        await _identity.DidNotReceive().AssignTenantAndRoleAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UserNotFound_ReturnsNotFound()
    {
        _identity.UserExistsAsync(_userId, Arg.Any<CancellationToken>()).Returns(false);

        var cmd    = new AssignTenantCommand(_userId, _tenantId, Roles.Agent);
        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.IsNotFound().Should().BeTrue();
    }

    [Fact]
    public async Task Handle_TenantNotFound_ReturnsNotFound()
    {
        _identity.UserExistsAsync(_userId, Arg.Any<CancellationToken>()).Returns(true);
        _identity.TenantExistsAsync(_tenantId, Arg.Any<CancellationToken>()).Returns(false);

        var cmd    = new AssignTenantCommand(_userId, _tenantId, Roles.TenantAdmin);
        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.IsNotFound().Should().BeTrue();
    }

    [Fact]
    public async Task Handle_IdentityServiceFailure_ReturnsError()
    {
        _identity.UserExistsAsync(_userId, Arg.Any<CancellationToken>()).Returns(true);
        _identity.TenantExistsAsync(_tenantId, Arg.Any<CancellationToken>()).Returns(true);
        _identity.AssignTenantAndRoleAsync(_userId, _tenantId, Roles.Agent, Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns((false, (IReadOnlyList<string>) ["Identity error"]));

        var cmd    = new AssignTenantCommand(_userId, _tenantId, Roles.Agent);
        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.IsError().Should().BeTrue();
    }
}
