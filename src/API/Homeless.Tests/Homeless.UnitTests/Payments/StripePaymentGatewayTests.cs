using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Homeless.Application.DTOs;
using Homeless.Application.Options;
using Homeless.Infrastructure.Payments;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace Homeless.UnitTests.Payments;

public sealed class StripePaymentGatewayTests
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly StripePaymentGateway _gateway;

    public StripePaymentGatewayTests()
    {
        _httpClientFactory = Substitute.For<IHttpClientFactory>();
        var options = Options.Create(
            new StripeOptions
            {
                BaseUrl = "http://localhost:8090",
                WebhookSecret = "whsec_test_secret",
            }
        );
        var logger = Substitute.For<ILogger<StripePaymentGateway>>();
        _gateway = new StripePaymentGateway(_httpClientFactory, options, logger);
    }

    [Fact]
    public async Task CreateCheckoutSessionAsync_WhenUsingSimulator_ShouldSendRealAmount()
    {
        JsonDocument? capturedPayload = null;
        var client = new HttpClient(
            new StubHttpMessageHandler(async request =>
            {
                var body = await request.Content!.ReadAsStringAsync();
                capturedPayload = JsonDocument.Parse(body);

                return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        """{"id":"cs_test_123","url":"http://checkout/cs_test_123","payment_intent":"pi_test_123"}""",
                        Encoding.UTF8,
                        "application/json"
                    ),
                };
            })
        )
        {
            BaseAddress = new Uri("http://localhost:8090"),
        };

        _httpClientFactory.CreateClient("Stripe").Returns(client);

        var result = await _gateway.CreateCheckoutSessionAsync(
            Guid.Parse("550e8400-e29b-41d4-a716-446655440000"),
            Guid.Parse("660e8400-e29b-41d4-a716-446655440000"),
            1495,
            "BRL",
            "http://success",
            "http://cancel",
            CancellationToken.None
        );

        result.SessionId.Should().Be("cs_test_123");
        capturedPayload.Should().NotBeNull();
        capturedPayload!.RootElement.GetProperty("amount").GetInt64().Should().Be(1495);
        capturedPayload.RootElement.GetProperty("currency").GetString().Should().Be("BRL");
    }

    [Fact]
    public async Task CreateSubscriptionCheckoutSessionAsync_WhenUsingSimulatorAndOneTimeAmount_ShouldSendCombinedAmount()
    {
        JsonDocument? capturedPayload = null;
        var client = new HttpClient(
            new StubHttpMessageHandler(async request =>
            {
                var body = await request.Content!.ReadAsStringAsync();
                capturedPayload = JsonDocument.Parse(body);

                return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        """{"id":"cs_test_combo","url":"http://checkout/cs_test_combo","payment_intent":"pi_test_combo","subscription":"sub_test_combo"}""",
                        Encoding.UTF8,
                        "application/json"
                    ),
                };
            })
        )
        {
            BaseAddress = new Uri("http://localhost:8090"),
        };

        _httpClientFactory.CreateClient("Stripe").Returns(client);

        var result = await _gateway.CreateSubscriptionCheckoutSessionAsync(
            new RecurringSubscriptionCheckoutRequest(
                TenantId: Guid.NewGuid(),
                TenantName: "Acme",
                CustomerEmail: "billing@acme.dev",
                PlanId: Guid.NewGuid(),
                PlanName: "Starter",
                Amount: 9700,
                Currency: "BRL",
                SuccessUrl: "http://success",
                CancelUrl: "http://cancel",
                StripePriceId: "price_starter",
                StripeCustomerId: null,
                OneTimeAmount: 3220,
                OneTimeDescription: "Addon purchase lead_nurturing"
            ),
            CancellationToken.None
        );

        result.SessionId.Should().Be("cs_test_combo");
        capturedPayload.Should().NotBeNull();
        capturedPayload!.RootElement.GetProperty("amount").GetInt64().Should().Be(12920);
        capturedPayload.RootElement.GetProperty("mode").GetString().Should().Be("subscription");
    }

    [Fact]
    public async Task CreateSubscriptionCheckoutSessionAsync_WhenUsingSimulator_ShouldSendCorrelationMetadata()
    {
        var correlationId = Guid.NewGuid();
        JsonDocument? capturedPayload = null;
        var client = new HttpClient(
            new StubHttpMessageHandler(async request =>
            {
                var body = await request.Content!.ReadAsStringAsync();
                capturedPayload = JsonDocument.Parse(body);

                return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        """{"id":"cs_test_corr","url":"http://checkout/cs_test_corr","payment_intent":"pi_test_corr","subscription":"sub_test_corr"}""",
                        Encoding.UTF8,
                        "application/json"
                    ),
                };
            })
        )
        {
            BaseAddress = new Uri("http://localhost:8090"),
        };

        _httpClientFactory.CreateClient("Stripe").Returns(client);

        var result = await _gateway.CreateSubscriptionCheckoutSessionAsync(
            new RecurringSubscriptionCheckoutRequest(
                TenantId: Guid.NewGuid(),
                TenantName: "Acme",
                CustomerEmail: "billing@acme.dev",
                PlanId: Guid.NewGuid(),
                PlanName: "Starter",
                Amount: 9700,
                Currency: "BRL",
                SuccessUrl: "http://success",
                CancelUrl: "http://cancel",
                StripePriceId: "price_starter",
                StripeCustomerId: null,
                CorrelationId: correlationId
            ),
            CancellationToken.None
        );

        result.SessionId.Should().Be("cs_test_corr");
        capturedPayload.Should().NotBeNull();
        capturedPayload!.RootElement
            .GetProperty("metadata")
            .GetProperty("correlation_id")
            .GetString()
            .Should()
            .Be(correlationId.ToString());
    }

    [Fact]
    public void FindManagedPlanProduct_ShouldReturnActiveProductWithMatchingPlanMetadata()
    {
        var planId = Guid.NewGuid();
        var products = new[]
        {
            new Stripe.Product
            {
                Id = "prod_other",
                Active = true,
                Metadata = new Dictionary<string, string>
                {
                    ["plan_id"] = Guid.NewGuid().ToString(),
                },
            },
            new Stripe.Product
            {
                Id = "prod_growth",
                Active = true,
                Metadata = new Dictionary<string, string> { ["plan_id"] = planId.ToString() },
            },
        };

        var recovered = StripePlanCatalogRecovery.FindManagedPlanProduct(planId, products);

        recovered.Should().NotBeNull();
        recovered!.Id.Should().Be("prod_growth");
    }

    [Fact]
    public void FindMatchingMonthlyPrice_ShouldReturnActiveMatchingRecurringPrice()
    {
        var prices = new[]
        {
            new Stripe.Price
            {
                Id = "price_old",
                Active = true,
                Currency = "brl",
                UnitAmount = 19900,
                Recurring = new Stripe.PriceRecurring { Interval = "month", IntervalCount = 1 },
            },
            new Stripe.Price
            {
                Id = "price_growth_v2",
                Active = true,
                Currency = "brl",
                UnitAmount = 24900,
                Recurring = new Stripe.PriceRecurring { Interval = "month", IntervalCount = 1 },
            },
        };

        var matching = StripePlanCatalogRecovery.FindMatchingMonthlyPrice(prices, "brl", 24900);

        matching.Should().NotBeNull();
        matching!.Id.Should().Be("price_growth_v2");
    }

    [Fact]
    public void VerifyWebhookSignature_ValidSignature_ReturnsTrue()
    {
        var secret = "whsec_test_secret";
        var payload = Encoding.UTF8.GetBytes("""{"type":"checkout.session.completed"}""");
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

        var signedPayload = $"{timestamp}.{Encoding.UTF8.GetString(payload)}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signedPayload));
        var signature = Convert.ToHexStringLower(hash);
        var header = $"t={timestamp},v1={signature}";

        var result = _gateway.VerifyWebhookSignature(payload, header, secret);

        result.Should().BeTrue();
    }

    [Fact]
    public void VerifyWebhookSignature_InvalidSignature_ReturnsFalse()
    {
        var payload = Encoding.UTF8.GetBytes("""{"type":"checkout.session.completed"}""");
        var header = "t=1234567890,v1=invalid_signature";

        var result = _gateway.VerifyWebhookSignature(payload, header, "whsec_test_secret");

        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyWebhookSignature_MalformedHeader_ReturnsFalse()
    {
        var payload = Encoding.UTF8.GetBytes("""{"type":"test"}""");

        var result = _gateway.VerifyWebhookSignature(payload, "garbage", "whsec_test_secret");

        result.Should().BeFalse();
    }

    [Fact]
    public void ParseWebhookEvent_ValidPayload_ReturnsCorrectEvent()
    {
        var payload = Encoding.UTF8.GetBytes(
            """
            {
                "id": "evt_test_123",
                "object": "event",
                "type": "checkout.session.completed",
                "created": 1234567890,
                "data": {
                    "object": {
                        "id": "cs_test_abc",
                        "payment_intent": "pi_test_def",
                        "status": "complete",
                        "amount_total": 9990,
                        "currency": "brl",
                        "metadata": {
                            "tenant_id": "550e8400-e29b-41d4-a716-446655440000",
                            "plan_id": "660e8400-e29b-41d4-a716-446655440000"
                        }
                    }
                }
            }
            """
        );

        var result = _gateway.ParseWebhookEvent(payload);

        result.EventId.Should().Be("evt_test_123");
        result.EventType.Should().Be("checkout.session.completed");
        result.SessionId.Should().Be("cs_test_abc");
        result.PaymentIntentId.Should().Be("pi_test_def");
        result.Status.Should().Be("complete");
        result.AmountTotal.Should().Be(9990);
        result.Currency.Should().Be("brl");
        result.Metadata.Should().ContainKey("tenant_id");
    }

    [Fact]
    public async Task GetBillingOverviewAsync_WhenCanceled_ShouldHideNextPaymentDateButPreserveAvailability()
    {
        var nextBillingDate = DateTime.UtcNow.AddDays(21);

        var result = await _gateway.GetBillingOverviewAsync(
            new BillingOverviewRequest(
                TenantId: Guid.NewGuid(),
                StripeCustomerId: "cus_demo",
                StripeSubscriptionId: null,
                PaymentStatus: "canceled",
                LastPaymentDate: DateTime.UtcNow.AddDays(-2),
                NextBillingDate: nextBillingDate,
                RecentTransactionsLimit: 5
            ),
            CancellationToken.None
        );

        result.SubscriptionStatus.Should().Be("canceled");
        result.NextPaymentDate.Should().BeNull();
        result.AvailabilityEndsAt.Should().Be(nextBillingDate);
    }

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, Task<HttpResponseMessage>> handler
    ) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        ) => handler(request);
    }
}
