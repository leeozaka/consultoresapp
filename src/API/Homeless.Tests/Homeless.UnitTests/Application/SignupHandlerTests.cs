using Ardalis.Result;
using FluentAssertions;
using NSubstitute;
using Homeless.Application.Interfaces;
using Homeless.Application.Sagas.Payment;
using Homeless.Application.UseCases.Onboarding;
using Homeless.Domain.Entities;
using Homeless.Domain.Interfaces.Repositories.Read;
using Homeless.Domain.Interfaces.Repositories.Write;
using Homeless.Domain.ValueObjects;
using Xunit;

namespace Homeless.UnitTests.Application;

public sealed class SignupHandlerTests
{
    private readonly ITenantReadRepository _tenantReadRepository = Substitute.For<ITenantReadRepository>();
    private readonly ITenantWriteRepository _tenantWriteRepository = Substitute.For<ITenantWriteRepository>();
    private readonly IPlanReadRepository _planReadRepository = Substitute.For<IPlanReadRepository>();
    private readonly IIdentityService _identityService = Substitute.For<IIdentityService>();
    private readonly IPaymentSagaService _paymentSagaService = Substitute.For<IPaymentSagaService>();
    private readonly IKeyValueStore _keyValueStore = Substitute.For<IKeyValueStore>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private SignupHandler CreateHandler() => new(
        _tenantReadRepository,
        _tenantWriteRepository,
        _planReadRepository,
        _identityService,
        _paymentSagaService,
        _keyValueStore,
        _unitOfWork);

    private static Plan BuildStarterPlan() => Plan.Create(
        name: "Starter",
        description: "Starter plan",
        price: Money.Create(9900L, Currency.BRL),
        maxProperties: 10,
        stripePriceId: "price_starter");

    private static SignupCommand BuildCommand(Guid? planId = null, string slug = "minha-imob") =>
        new(
            AgencyName: "Minha Imobiliária",
            Slug: slug,
            ContactEmail: "contato@minha-imob.com.br",
            ContactPhone: null,
            PlanId: planId ?? Guid.NewGuid(),
            FirstName: "João",
            LastName: "Silva",
            Email: "joao@minha-imob.com.br",
            Password: "Senha@123",
            SuccessUrl: "https://consultor.app/onboarding/status/tenant-id",
            CancelUrl: "https://consultor.app/onboarding/signup"
        );

    [Fact]
    public async Task Handle_WhenSlugAlreadyTaken_ShouldReturnConflict()
    {
        // An Active tenant with that slug means it's permanently taken (not resumable)
        var activeTenant = Tenant.Create("Outra Imob", "minha-imob", "outro@imob.com.br");
        activeTenant.Activate();

        _tenantReadRepository
            .GetBySlugAsync("minha-imob", Arg.Any<CancellationToken>())
            .Returns(activeTenant);

        var handler = CreateHandler();
        var result = await handler.Handle(BuildCommand(), CancellationToken.None);

        result.Status.Should().Be(ResultStatus.Conflict);
        result.Errors.Should().ContainMatch("*'minha-imob'*");
    }

    [Fact]
    public async Task Handle_WhenEmailAlreadyExists_ShouldReturnConflict()
    {
        _tenantReadRepository
            .GetBySlugAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Tenant?)null);

        _identityService
            .EmailExistsAsync("joao@minha-imob.com.br", Arg.Any<CancellationToken>())
            .Returns(true);

        var handler = CreateHandler();
        var result = await handler.Handle(BuildCommand(), CancellationToken.None);

        result.Status.Should().Be(ResultStatus.Conflict);
        result.Errors.Should().ContainMatch("*'joao@minha-imob.com.br'*");
    }

    [Fact]
    public async Task Handle_WhenPlanNotFound_ShouldReturnNotFound()
    {
        _tenantReadRepository
            .GetBySlugAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Tenant?)null);

        _identityService
            .EmailExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        _planReadRepository
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Plan?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(BuildCommand(), CancellationToken.None);

        result.Status.Should().Be(ResultStatus.NotFound);
    }

    [Fact]
    public async Task Handle_WhenAllValid_ShouldCreateTenantAndUserAndReturnCheckoutUrl()
    {
        var plan = BuildStarterPlan();
        var userId = Guid.NewGuid();
        const string stripeSessionId = "cs_test_abc123";
        const string checkoutUrl = "https://checkout.stripe.test/cs_test_abc123";

        _tenantReadRepository
            .GetBySlugAsync("minha-imob", Arg.Any<CancellationToken>())
            .Returns((Tenant?)null);

        _identityService
            .EmailExistsAsync("joao@minha-imob.com.br", Arg.Any<CancellationToken>())
            .Returns(false);

        _planReadRepository
            .GetByIdAsync(plan.Id, Arg.Any<CancellationToken>())
            .Returns(plan);

        _identityService
            .CreateUserAsync(
                "joao@minha-imob.com.br", "Senha@123", "João", "Silva",
                Arg.Any<CancellationToken>())
            .Returns((true, userId, (IReadOnlyList<string>)Array.Empty<string>()));

        _identityService
            .AssignTenantAndRoleAsync(
                userId, Arg.Any<Guid>(), "TenantAdmin", false,
                Arg.Any<CancellationToken>())
            .Returns((true, (IReadOnlyList<string>)Array.Empty<string>()));

        _paymentSagaService
            .InitiateCheckoutAsync(
                Arg.Any<Guid>(), plan.Id,
                plan.Price.AmountInCents, plan.Price.Currency.Code,
                Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(new PaymentSagaResult(
                Success: true,
                SessionUrl: checkoutUrl,
                StripeSessionId: stripeSessionId,
                ErrorMessage: null));

        var handler = CreateHandler();
        var result = await handler.Handle(BuildCommand(plan.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CheckoutUrl.Should().Be(checkoutUrl);
        result.Value.UserId.Should().Be(userId);
        result.Value.DismissToken.Should().NotBeNullOrEmpty();

        // Verify user created inactive (activate=false)
        await _identityService.Received(1).AssignTenantAndRoleAsync(
            userId, Arg.Any<Guid>(), "TenantAdmin", false, Arg.Any<CancellationToken>());

        // Verify signup marker stored in key-value store
        await _keyValueStore.Received(1).SetAsync(
            $"signup-checkout:{stripeSessionId}",
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());

        // Verify dismiss token stored in Redis with TTL
        await _keyValueStore.Received(1).SetAsync(
            Arg.Is<string>(k => k.StartsWith("signup-dismiss:")),
            Arg.Any<string>(),
            TimeSpan.FromHours(48),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenPaymentInitiationFails_ShouldReturnError()
    {
        var plan = BuildStarterPlan();
        var userId = Guid.NewGuid();

        _tenantReadRepository
            .GetBySlugAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Tenant?)null);

        _identityService
            .EmailExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        _planReadRepository
            .GetByIdAsync(plan.Id, Arg.Any<CancellationToken>())
            .Returns(plan);

        _identityService
            .CreateUserAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((true, userId, (IReadOnlyList<string>)Array.Empty<string>()));

        _identityService
            .AssignTenantAndRoleAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns((true, (IReadOnlyList<string>)Array.Empty<string>()));

        _paymentSagaService
            .InitiateCheckoutAsync(
                Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<long>(), Arg.Any<string>(),
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new PaymentSagaResult(
                Success: false,
                SessionUrl: null,
                StripeSessionId: null,
                ErrorMessage: "Stripe unavailable"));

        var handler = CreateHandler();
        var result = await handler.Handle(BuildCommand(plan.Id), CancellationToken.None);

        result.Status.Should().Be(ResultStatus.Error);
        result.Errors.Should().ContainMatch("*Stripe unavailable*");
    }
}
