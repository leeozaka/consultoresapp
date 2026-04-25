using Ardalis.Result;
using FluentAssertions;
using NSubstitute;
using Homeless.Application.Interfaces;
using Homeless.Application.UseCases.Tenants;
using Homeless.Domain.Entities;
using Homeless.Domain.Interfaces.Repositories.Read;
using Homeless.Domain.Interfaces.Repositories.Write;
using Xunit;

namespace Homeless.UnitTests.Application;

public class SetTenantCustomDomainHandlerTests
{
    private readonly ITenantReadRepository _tenantReadRepository = Substitute.For<ITenantReadRepository>();
    private readonly ITenantWriteRepository _tenantWriteRepository = Substitute.For<ITenantWriteRepository>();
    private readonly ICacheService _cacheService = Substitute.For<ICacheService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ISiteBuildChannel _siteBuildChannel = Substitute.For<ISiteBuildChannel>();
    private readonly ITenantOriginService _tenantOriginService = Substitute.For<ITenantOriginService>();
    private readonly IOidcClientRedirectService _oidcClientRedirectService = Substitute.For<IOidcClientRedirectService>();

    private SetTenantCustomDomainHandler CreateHandler() =>
        new(
            _tenantReadRepository,
            _tenantWriteRepository,
            _cacheService,
            _unitOfWork,
            _siteBuildChannel,
            _tenantOriginService,
            _oidcClientRedirectService);

    [Fact]
    public async Task Handle_WhenEntitled_ShouldUpdateDomainAndTriggerProvisioning()
    {
        var tenant = Tenant.Create("Acme", "acme", "admin@acme.dev");
        tenant.UpdateEntitlements(new Dictionary<string, object>
        {
            ["custom_domain"] = true
        });

        _tenantReadRepository.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>())
            .Returns(tenant);

        _tenantReadRepository.ExistsByCustomDomainAsync("portal.acme.com", Arg.Any<CancellationToken>())
            .Returns(false);

        var handler = CreateHandler();

        var result = await handler.Handle(
            new SetTenantCustomDomainCommand(tenant.Id, "portal.acme.com"),
            CancellationToken.None);

        result.Status.Should().Be(ResultStatus.Ok);
        result.Value.CustomDomain.Should().Be("portal.acme.com");

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _siteBuildChannel.Received(1)
            .WriteAsync(Arg.Is<SiteBuildMessage>(m =>
                m.TenantId == tenant.Id &&
                m.CustomDomain == "portal.acme.com"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenNotEntitled_ShouldReturnForbidden()
    {
        var tenant = Tenant.Create("Acme", "acme", "admin@acme.dev");

        _tenantReadRepository.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>())
            .Returns(tenant);

        var handler = CreateHandler();

        var result = await handler.Handle(
            new SetTenantCustomDomainCommand(tenant.Id, "portal.acme.com"),
            CancellationToken.None);

        result.Status.Should().Be(ResultStatus.Forbidden);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
