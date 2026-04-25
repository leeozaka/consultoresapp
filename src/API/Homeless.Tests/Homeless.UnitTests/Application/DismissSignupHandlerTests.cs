using Ardalis.Result;
using FluentAssertions;
using NSubstitute;
using Homeless.Application.Interfaces;
using Homeless.Application.UseCases.Onboarding;
using Homeless.Domain.Entities;
using Homeless.Domain.Interfaces.Repositories.Read;
using Homeless.Domain.Interfaces.Repositories.Write;
using Xunit;

namespace Homeless.UnitTests.Application;

public sealed class DismissSignupHandlerTests
{
    private readonly ITenantReadRepository _tenantRead = Substitute.For<ITenantReadRepository>();
    private readonly ITenantWriteRepository _tenantWrite = Substitute.For<ITenantWriteRepository>();
    private readonly IIdentityService _identityService = Substitute.For<IIdentityService>();
    private readonly IOnboardingStatusStream _onboardingStream = Substitute.For<IOnboardingStatusStream>();
    private readonly IKeyValueStore _keyValueStore = Substitute.For<IKeyValueStore>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private DismissSignupHandler CreateHandler() => new(
        _tenantRead, _tenantWrite, _identityService,
        _onboardingStream, _keyValueStore, _unitOfWork);

    [Fact]
    public async Task Handle_WithValidToken_ShouldDeleteTenantAndUser()
    {
        var userId = Guid.NewGuid();
        var tenant = Tenant.Create("Imob", "imob", "c@imob.com.br");
        tenant.SetOwnerUserId(userId);
        const string token = "valid-token-abc123";

        _tenantRead.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(tenant);
        _keyValueStore.GetAsync<string>($"signup-dismiss:{tenant.Id}", Arg.Any<CancellationToken>()).Returns(token);
        _identityService.DeleteUserAsync(userId, Arg.Any<CancellationToken>()).Returns(true);

        var result = await CreateHandler().Handle(
            new DismissSignupCommand(tenant.Id, token), CancellationToken.None);

        result.Status.Should().Be(ResultStatus.NoContent);

        await _onboardingStream.Received(1).PublishAsync(
            Arg.Is<OnboardingStatusEvent>(e => e.TenantId == tenant.Id && e.Status == "abandoned"),
            Arg.Any<CancellationToken>());
        await _identityService.Received(1).DeleteUserAsync(userId, Arg.Any<CancellationToken>());
        await _tenantWrite.Received(1).DeleteAsync(tenant.Id, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _keyValueStore.Received(1).RemoveAsync($"signup-dismiss:{tenant.Id}", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithInvalidToken_ShouldReturnUnauthorized()
    {
        var tenant = Tenant.Create("Imob", "imob", "c@imob.com.br");
        tenant.SetOwnerUserId(Guid.NewGuid());

        _tenantRead.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(tenant);
        _keyValueStore.GetAsync<string>($"signup-dismiss:{tenant.Id}", Arg.Any<CancellationToken>()).Returns("correct-token");

        var result = await CreateHandler().Handle(
            new DismissSignupCommand(tenant.Id, "wrong-token"), CancellationToken.None);

        result.Status.Should().Be(ResultStatus.Unauthorized);
        await _tenantWrite.DidNotReceive().DeleteAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenNoTokenStored_ShouldReturnUnauthorized()
    {
        var tenant = Tenant.Create("Imob", "imob", "c@imob.com.br");
        tenant.SetOwnerUserId(Guid.NewGuid());

        _tenantRead.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(tenant);
        _keyValueStore.GetAsync<string>($"signup-dismiss:{tenant.Id}", Arg.Any<CancellationToken>()).Returns((string?)null);

        var result = await CreateHandler().Handle(
            new DismissSignupCommand(tenant.Id, "any-token"), CancellationToken.None);

        result.Status.Should().Be(ResultStatus.Unauthorized);
    }

    [Fact]
    public async Task Handle_WhenTenantNotFound_ShouldReturnNotFound()
    {
        var tenantId = Guid.NewGuid();
        _tenantRead.GetByIdAsync(tenantId, Arg.Any<CancellationToken>()).Returns((Tenant?)null);

        var result = await CreateHandler().Handle(
            new DismissSignupCommand(tenantId, "token"), CancellationToken.None);

        result.Status.Should().Be(ResultStatus.NotFound);
    }

    [Fact]
    public async Task Handle_WhenTenantIsNotPending_ShouldReturnConflict()
    {
        var tenant = Tenant.Create("Imob", "imob", "c@imob.com.br");
        tenant.Activate();

        _tenantRead.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(tenant);

        var result = await CreateHandler().Handle(
            new DismissSignupCommand(tenant.Id, "token"), CancellationToken.None);

        result.Status.Should().Be(ResultStatus.Conflict);
    }

    [Fact]
    public async Task Handle_WhenTenantHasNoOwner_ShouldDeleteTenantWithoutDeletingUser()
    {
        var tenant = Tenant.Create("Imob", "imob", "c@imob.com.br");
        const string token = "valid-token";

        _tenantRead.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(tenant);
        _keyValueStore.GetAsync<string>($"signup-dismiss:{tenant.Id}", Arg.Any<CancellationToken>()).Returns(token);

        var result = await CreateHandler().Handle(
            new DismissSignupCommand(tenant.Id, token), CancellationToken.None);

        result.Status.Should().Be(ResultStatus.NoContent);
        await _identityService.DidNotReceive().DeleteUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _tenantWrite.Received(1).DeleteAsync(tenant.Id, Arg.Any<CancellationToken>());
    }
}
