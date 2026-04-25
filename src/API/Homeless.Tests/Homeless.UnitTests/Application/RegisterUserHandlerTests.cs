using Ardalis.Result;
using FluentAssertions;
using NSubstitute;
using Homeless.Application.Interfaces;
using Homeless.Application.UseCases.Auth;
using Xunit;

namespace Homeless.UnitTests.Application;

public class RegisterUserHandlerTests
{
    private readonly IIdentityService _identity = Substitute.For<IIdentityService>();
    private readonly RegisterUserHandler _handler;

    public RegisterUserHandlerTests()
    {
        _handler = new RegisterUserHandler(_identity);
    }

    [Fact]
    public async Task Handle_NewEmail_CreatesUserAndReturnsCreated()
    {
        var newId = Guid.NewGuid();
        _identity.EmailExistsAsync("new@example.com", Arg.Any<CancellationToken>()).Returns(false);
        _identity.CreateUserAsync("new@example.com", "P@ssw0rd!", "João", "Silva", Arg.Any<CancellationToken>())
            .Returns((true, newId, Array.Empty<string>()));

        var cmd    = new RegisterUserCommand("new@example.com", "P@ssw0rd!", "João", "Silva");
        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(newId);
        result.Value.Email.Should().Be("new@example.com");
    }

    [Fact]
    public async Task Handle_DuplicateEmail_ReturnsConflict()
    {
        _identity.EmailExistsAsync("dup@example.com", Arg.Any<CancellationToken>()).Returns(true);

        var cmd    = new RegisterUserCommand("dup@example.com", "P@ssw0rd!", null, null);
        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.IsError().Should().BeTrue();
        await _identity.DidNotReceive().CreateUserAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_IdentityRejectsPassword_ReturnsInvalid()
    {
        _identity.EmailExistsAsync("weak@example.com", Arg.Any<CancellationToken>()).Returns(false);
        _identity.CreateUserAsync("weak@example.com", "123", null, null, Arg.Any<CancellationToken>())
            .Returns((false, Guid.Empty, (IReadOnlyList<string>) ["Password too short"]));

        var cmd    = new RegisterUserCommand("weak@example.com", "123", null, null);
        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.IsInvalid().Should().BeTrue();
    }
}
