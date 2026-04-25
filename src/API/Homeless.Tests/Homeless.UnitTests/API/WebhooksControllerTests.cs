using System.Text;
using Ardalis.Result;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Homeless.API.Controllers;
using Homeless.Application.DTOs;
using Homeless.Application.Interfaces;
using Homeless.Application.Options;
using Homeless.Application.UseCases.Webhooks;
using Xunit;

namespace Homeless.UnitTests.API;

public sealed class WebhooksControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly IPaymentGateway _paymentGateway = Substitute.For<IPaymentGateway>();
    private readonly ICacheService _cacheService = Substitute.For<ICacheService>();
    private readonly ILogger<WebhooksController> _logger = Substitute.For<ILogger<WebhooksController>>();
    private readonly IOptions<StripeOptions> _stripeOptions = Options.Create(new StripeOptions
    {
        WebhookSecret = "whsec_test"
    });

    private WebhooksController CreateController()
    {
        var controller = new WebhooksController(
            _mediator,
            _paymentGateway,
            _cacheService,
            _stripeOptions,
            _logger);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        controller.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{}"));
        return controller;
    }

    [Fact]
    public async Task StripeWebhook_WhenSignatureInvalid_ShouldReturnUnauthorized()
    {
        _paymentGateway.VerifyWebhookSignature(Arg.Any<byte[]>(), "bad_sig", "whsec_test")
            .Returns(false);

        var result = await CreateController().StripeWebhook("bad_sig", CancellationToken.None);

        result.Should().BeOfType<UnauthorizedResult>();
        await _mediator.DidNotReceive().Send(Arg.Any<ProcessStripeWebhookCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StripeWebhook_WhenEventAlreadyProcessed_ShouldReturnOkWithoutDispatching()
    {
        _paymentGateway.VerifyWebhookSignature(Arg.Any<byte[]>(), "sig_test", "whsec_test")
            .Returns(true);
        _paymentGateway.ParseWebhookEvent(Arg.Any<byte[]>())
            .Returns(new PaymentWebhookEvent(
                EventId: "evt_dup_123",
                EventType: "invoice.paid",
                SessionId: string.Empty,
                PaymentIntentId: string.Empty,
                SubscriptionId: string.Empty,
                Status: "paid",
                AmountTotal: 9900,
                Currency: "brl",
                Metadata: null));

        _cacheService.GetAsync<string>(CacheKeys.WebhookEvent("evt_dup_123"), Arg.Any<CancellationToken>())
            .Returns("processed");

        var result = await CreateController().StripeWebhook("sig_test", CancellationToken.None);

        result.Should().BeOfType<OkResult>();
        await _mediator.DidNotReceive().Send(Arg.Any<ProcessStripeWebhookCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StripeWebhook_WhenValidAndNew_ShouldDispatchCommandAndReturnOk()
    {
        _paymentGateway.VerifyWebhookSignature(Arg.Any<byte[]>(), "sig_test", "whsec_test")
            .Returns(true);

        var webhookEvent = new PaymentWebhookEvent(
            EventId: "evt_new_123",
            EventType: "checkout.session.completed",
            SessionId: "cs_123",
            PaymentIntentId: "pi_123",
            SubscriptionId: string.Empty,
            Status: "complete",
            AmountTotal: 9900,
            Currency: "brl",
            Metadata: new Dictionary<string, string> { ["tenant_id"] = Guid.NewGuid().ToString() });

        _paymentGateway.ParseWebhookEvent(Arg.Any<byte[]>()).Returns(webhookEvent);

        _cacheService.GetAsync<string>(CacheKeys.WebhookEvent("evt_new_123"), Arg.Any<CancellationToken>())
            .Returns((string?)null);

        _mediator.Send(Arg.Any<ProcessStripeWebhookCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await CreateController().StripeWebhook("sig_test", CancellationToken.None);

        result.Should().BeOfType<OkResult>();
        await _mediator.Received(1).Send(
            Arg.Is<ProcessStripeWebhookCommand>(cmd => cmd.Event.EventId == "evt_new_123"),
            Arg.Any<CancellationToken>());
        await _cacheService.Received(1).SetAsync(
            CacheKeys.WebhookEvent("evt_new_123"),
            "processed",
            TimeSpan.FromHours(24),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StripeWebhook_WhenHandlerFails_ShouldNotMarkEventProcessed()
    {
        _paymentGateway.VerifyWebhookSignature(Arg.Any<byte[]>(), "sig_test", "whsec_test")
            .Returns(true);

        var webhookEvent = new PaymentWebhookEvent(
            EventId: "evt_retry_123",
            EventType: "checkout.session.completed",
            SessionId: "cs_retry",
            PaymentIntentId: "pi_retry",
            SubscriptionId: string.Empty,
            Status: "complete",
            AmountTotal: 9900,
            Currency: "brl",
            Metadata: new Dictionary<string, string> { ["tenant_id"] = Guid.NewGuid().ToString() });

        _paymentGateway.ParseWebhookEvent(Arg.Any<byte[]>()).Returns(webhookEvent);
        _cacheService.GetAsync<string>(CacheKeys.WebhookEvent("evt_retry_123"), Arg.Any<CancellationToken>())
            .Returns((string?)null);
        _mediator.Send(Arg.Any<ProcessStripeWebhookCommand>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException<Result>(new InvalidOperationException("handler failed")));

        var act = async () => await CreateController().StripeWebhook("sig_test", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        await _cacheService.DidNotReceive().SetAsync(
            CacheKeys.WebhookEvent("evt_retry_123"),
            "processed",
            TimeSpan.FromHours(24),
            Arg.Any<CancellationToken>());
    }
}
