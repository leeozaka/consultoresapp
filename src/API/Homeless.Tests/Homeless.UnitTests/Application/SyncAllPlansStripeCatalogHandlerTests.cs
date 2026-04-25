using FluentAssertions;
using NSubstitute;
using Homeless.Application.DTOs;
using Homeless.Application.Interfaces;
using Homeless.Application.UseCases.Plans;
using Homeless.Domain.Entities;
using Homeless.Domain.Interfaces.Repositories.Read;
using Homeless.Domain.Interfaces.Repositories.Write;
using Homeless.Domain.ValueObjects;
using Xunit;

namespace Homeless.UnitTests.Application;

public sealed class SyncAllPlansStripeCatalogHandlerTests
{
    private readonly IPlanReadRepository _planReadRepository = Substitute.For<IPlanReadRepository>();
    private readonly IPlanWriteRepository _planWriteRepository = Substitute.For<IPlanWriteRepository>();
    private readonly IStripePlanCatalogGateway _stripePlanCatalogGateway = Substitute.For<IStripePlanCatalogGateway>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private SyncAllPlansStripeCatalogHandler CreateHandler() =>
        new(
            _planReadRepository,
            _planWriteRepository,
            _stripePlanCatalogGateway,
            _unitOfWork);

    [Fact]
    public async Task Handle_ShouldSyncEveryPlanAndPersistUpdatedStripePriceIds()
    {
        var starter = Plan.Create("Starter", "Starter plan", Money.Create(9900L, Currency.BRL), 25);
        var growth = Plan.Create("Growth", "Growth plan", Money.Create(19900L, Currency.BRL), 100, stripePriceId: "price_growth_v1");

        _planReadRepository.GetAllPlansAsync(Arg.Any<CancellationToken>())
            .Returns([starter, growth]);

        _stripePlanCatalogGateway.UpsertPlanCatalogAsync(
                Arg.Is<PlanStripeCatalogRequest>(x => x.PlanId == starter.Id),
                Arg.Any<CancellationToken>())
            .Returns(new PlanStripeCatalogResult("price_starter_v1", "prod_starter", true, true));

        _stripePlanCatalogGateway.UpsertPlanCatalogAsync(
                Arg.Is<PlanStripeCatalogRequest>(x => x.PlanId == growth.Id),
                Arg.Any<CancellationToken>())
            .Returns(new PlanStripeCatalogResult("price_growth_v1", "prod_growth", false, false));

        var handler = CreateHandler();

        var result = await handler.Handle(new SyncAllPlansStripeCatalogCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        starter.StripePriceId.Should().Be("price_starter_v1");
        growth.StripePriceId.Should().Be("price_growth_v1");

        await _planWriteRepository.Received(1).UpdateAsync(starter, Arg.Any<CancellationToken>());
        await _planWriteRepository.Received(1).UpdateAsync(growth, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
