using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Homeless.Application.DTOs;
using Homeless.Application.Interfaces;
using Homeless.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;
using StripeBillingPortalSessionCreateOptions = Stripe.BillingPortal.SessionCreateOptions;
using StripeBillingPortalSessionService = Stripe.BillingPortal.SessionService;
using StripeCheckoutSessionCreateOptions = Stripe.Checkout.SessionCreateOptions;
using StripeCheckoutSessionService = Stripe.Checkout.SessionService;

namespace Homeless.Infrastructure.Payments;

public sealed class StripePaymentGateway(
    IHttpClientFactory httpClientFactory,
    IOptions<StripeOptions> stripeOptions,
    ILogger<StripePaymentGateway> logger
) : IPaymentGateway, IStripePlanCatalogGateway
{
    private readonly StripeOptions _options = stripeOptions.Value;
    private const string SubscriptionMode = "subscription";
    private const string MonthlyInterval = "month";
    private const string PlanIdMetadataKey = "plan_id";
    private const string CorrelationIdMetadataKey = "correlation_id";

    private StripeClient CreateStripeClient() => new(_options.SecretKey);

    public async Task<PlanStripeCatalogResult> UpsertPlanCatalogAsync(
        PlanStripeCatalogRequest request,
        CancellationToken cancellationToken = default
    )
    {
        if (IsSimulatorEndpoint())
            return BuildSimulatorPlanCatalog(request);

        if (string.IsNullOrWhiteSpace(_options.SecretKey))
            throw new InvalidOperationException(
                "Stripe secret key is not configured for catalog management."
            );

        var client = CreateStripeClient();
        var productService = new ProductService(client);
        var priceService = new PriceService(client);
        var currency = request.CurrencyCode.ToLowerInvariant();
        var unitAmount = ToMinorUnits(request.PricePerMonth);
        var metadata = BuildPlanMetadata(request.PlanId);

        if (string.IsNullOrWhiteSpace(request.StripePriceId))
        {
            var recoveredProduct = await TryRecoverPlanProductAsync(
                    productService,
                    request.PlanId,
                    cancellationToken
                )
                .ConfigureAwait(false);

            if (recoveredProduct is not null)
            {
                await productService
                    .UpdateAsync(
                        recoveredProduct.Id,
                        new ProductUpdateOptions
                        {
                            Name = request.Name,
                            Description = request.Description,
                            Metadata = metadata,
                            Active = true,
                        },
                        cancellationToken: cancellationToken
                    )
                    .ConfigureAwait(false);

                var activePrices = await priceService
                    .ListAsync(
                        new PriceListOptions
                        {
                            Product = recoveredProduct.Id,
                            Active = true,
                            Limit = 100,
                        },
                        cancellationToken: cancellationToken
                    )
                    .ConfigureAwait(false);

                var matchingPrice = StripePlanCatalogRecovery.FindMatchingMonthlyPrice(
                    activePrices.Data,
                    currency,
                    unitAmount
                );

                if (matchingPrice is not null)
                    return new PlanStripeCatalogResult(
                        matchingPrice.Id,
                        recoveredProduct.Id,
                        CreatedProduct: false,
                        CreatedPrice: false
                    );

                var recoveredPrice = await priceService
                    .CreateAsync(
                        new PriceCreateOptions
                        {
                            Product = recoveredProduct.Id,
                            Currency = currency,
                            UnitAmount = unitAmount,
                            Recurring = new PriceRecurringOptions { Interval = MonthlyInterval },
                            Metadata = metadata,
                            Active = true,
                        },
                        cancellationToken: cancellationToken
                    )
                    .ConfigureAwait(false);

                foreach (var activePrice in activePrices.Data.Where(price => price.Active))
                {
                    await priceService
                        .UpdateAsync(
                            activePrice.Id,
                            new PriceUpdateOptions { Active = false, Metadata = metadata },
                            cancellationToken: cancellationToken
                        )
                        .ConfigureAwait(false);
                }

                return new PlanStripeCatalogResult(
                    recoveredPrice.Id,
                    recoveredProduct.Id,
                    CreatedProduct: false,
                    CreatedPrice: true
                );
            }

            var product = await productService
                .CreateAsync(
                    new ProductCreateOptions
                    {
                        Name = request.Name,
                        Description = request.Description,
                        Metadata = metadata,
                        Active = true,
                    },
                    cancellationToken: cancellationToken
                )
                .ConfigureAwait(false);

            var price = await priceService
                .CreateAsync(
                    new PriceCreateOptions
                    {
                        Product = product.Id,
                        Currency = currency,
                        UnitAmount = unitAmount,
                        Recurring = new PriceRecurringOptions { Interval = MonthlyInterval },
                        Metadata = metadata,
                        Active = true,
                    },
                    cancellationToken: cancellationToken
                )
                .ConfigureAwait(false);

            return new PlanStripeCatalogResult(
                price.Id,
                product.Id,
                CreatedProduct: true,
                CreatedPrice: true
            );
        }

        var existingPrice = await priceService
            .GetAsync(request.StripePriceId, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        var productId = existingPrice.ProductId;
        if (string.IsNullOrWhiteSpace(productId))
            throw new InvalidOperationException(
                $"Stripe price '{request.StripePriceId}' is missing an associated product."
            );

        await productService
            .UpdateAsync(
                productId,
                new ProductUpdateOptions
                {
                    Name = request.Name,
                    Description = request.Description,
                    Metadata = metadata,
                    Active = true,
                },
                cancellationToken: cancellationToken
            )
            .ConfigureAwait(false);

        if (MatchesMonthlyCatalog(existingPrice, currency, unitAmount))
            return new PlanStripeCatalogResult(
                existingPrice.Id,
                productId,
                CreatedProduct: false,
                CreatedPrice: false
            );

        var newPrice = await priceService
            .CreateAsync(
                new PriceCreateOptions
                {
                    Product = productId,
                    Currency = currency,
                    UnitAmount = unitAmount,
                    Recurring = new PriceRecurringOptions { Interval = MonthlyInterval },
                    Metadata = metadata,
                    Active = true,
                },
                cancellationToken: cancellationToken
            )
            .ConfigureAwait(false);

        await priceService
            .UpdateAsync(
                existingPrice.Id,
                new PriceUpdateOptions { Active = false, Metadata = metadata },
                cancellationToken: cancellationToken
            )
            .ConfigureAwait(false);

        return new PlanStripeCatalogResult(
            newPrice.Id,
            productId,
            CreatedProduct: false,
            CreatedPrice: true
        );
    }

    // removed — IStripeAddonCatalogGateway

    // removed — addon only

    // removed — addon only

    // removed — addon only

    // removed — IStripeAddonCatalogGateway

    public async Task<CheckoutSessionResult> CreateCheckoutSessionAsync(
        Guid tenantId,
        Guid planId,
        long amount,
        string currency,
        string successUrl,
        string cancelUrl,
        CancellationToken cancellationToken = default
    )
    {
        var metadata = new Dictionary<string, string>
        {
            ["tenant_id"] = tenantId.ToString(),
            [PlanIdMetadataKey] = planId.ToString(),
            ["checkout_kind"] = "addon",
        };

        if (IsSimulatorEndpoint())
            return await CreateSimulatorCheckoutAsync(
                    new SimulatorCheckoutRequest(
                        Mode: "payment",
                        Amount: amount,
                        Currency: currency,
                        SuccessUrl: successUrl,
                        CancelUrl: cancelUrl,
                        Metadata: metadata,
                        CustomerId: null
                    ),
                    cancellationToken
                )
                .ConfigureAwait(false);

        var service = new StripeCheckoutSessionService(CreateStripeClient());
        var session = await service
            .CreateAsync(
                new StripeCheckoutSessionCreateOptions
                {
                    Mode = "payment",
                    SuccessUrl = successUrl,
                    CancelUrl = cancelUrl,
                    Metadata = metadata,
                    LineItems =
                    [
                        new SessionLineItemOptions
                        {
                            Quantity = 1,
                            PriceData = new SessionLineItemPriceDataOptions
                            {
                                Currency = currency.ToLowerInvariant(),
                                UnitAmount = amount,
                                ProductData = new SessionLineItemPriceDataProductDataOptions
                                {
                                    Name = $"Addon purchase {planId}",
                                    Metadata = metadata,
                                },
                            },
                        },
                    ],
                },
                cancellationToken: cancellationToken
            )
            .ConfigureAwait(false);

        return new CheckoutSessionResult(
            SessionId: session.Id,
            SessionUrl: session.Url ?? string.Empty,
            PaymentIntentId: session.PaymentIntentId ?? string.Empty,
            CustomerId: session.CustomerId ?? string.Empty,
            SubscriptionId: session.SubscriptionId ?? string.Empty
        );
    }

    public async Task<CheckoutSessionResult> CreateSubscriptionCheckoutSessionAsync(
        RecurringSubscriptionCheckoutRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var metadata = new Dictionary<string, string>
        {
            ["tenant_id"] = request.TenantId.ToString(),
            [PlanIdMetadataKey] = request.PlanId.ToString(),
            ["checkout_kind"] = "plan",
        };
        if (request.CorrelationId is not null)
            metadata[CorrelationIdMetadataKey] = request.CorrelationId.Value.ToString();

        if (IsSimulatorEndpoint())
            return await CreateSimulatorCheckoutAsync(
                    new SimulatorCheckoutRequest(
                        Mode: SubscriptionMode,
                        Amount: request.Amount + request.OneTimeAmount.GetValueOrDefault(),
                        Currency: request.Currency,
                        SuccessUrl: request.SuccessUrl,
                        CancelUrl: request.CancelUrl,
                        Metadata: metadata,
                        CustomerId: request.StripeCustomerId
                    ),
                    cancellationToken
                )
                .ConfigureAwait(false);

        var options = new StripeCheckoutSessionCreateOptions
        {
            Mode = SubscriptionMode,
            SuccessUrl = request.SuccessUrl,
            CancelUrl = request.CancelUrl,
            ClientReferenceId = request.TenantId.ToString(),
            Customer = request.StripeCustomerId,
            CustomerEmail = string.IsNullOrWhiteSpace(request.StripeCustomerId)
                ? request.CustomerEmail
                : null,
            Metadata = metadata,
            SubscriptionData = new SessionSubscriptionDataOptions { Metadata = metadata },
            LineItems = CreateSubscriptionLineItems(request, metadata),
        };

        var service = new StripeCheckoutSessionService(CreateStripeClient());
        var session = await service
            .CreateAsync(options, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return new CheckoutSessionResult(
            SessionId: session.Id,
            SessionUrl: session.Url ?? string.Empty,
            PaymentIntentId: session.PaymentIntentId ?? string.Empty,
            CustomerId: session.CustomerId ?? string.Empty,
            SubscriptionId: session.SubscriptionId ?? string.Empty
        );
    }

    public async Task<BillingOverviewResult> GetBillingOverviewAsync(
        BillingOverviewRequest request,
        CancellationToken cancellationToken = default
    )
    {
        if (IsSimulatorEndpoint() || string.IsNullOrWhiteSpace(_options.SecretKey))
            return BuildFallbackOverview(request);

        var client = CreateStripeClient();
        var subscriptionService = new SubscriptionService(client);
        var invoiceService = new InvoiceService(client);

        Subscription? subscription = null;
        if (!string.IsNullOrWhiteSpace(request.StripeSubscriptionId))
        {
            try
            {
                subscription = await subscriptionService
                    .GetAsync(request.StripeSubscriptionId, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (StripeException ex)
            {
                logger.LogWarning(
                    ex,
                    "Failed to fetch Stripe subscription {SubscriptionId}",
                    request.StripeSubscriptionId
                );
            }
        }

        StripeList<Invoice>? invoices = null;
        if (!string.IsNullOrWhiteSpace(request.StripeCustomerId))
        {
            try
            {
                invoices = await invoiceService
                    .ListAsync(
                        new InvoiceListOptions
                        {
                            Customer = request.StripeCustomerId,
                            Limit = request.RecentTransactionsLimit,
                        },
                        cancellationToken: cancellationToken
                    )
                    .ConfigureAwait(false);
            }
            catch (StripeException ex)
            {
                logger.LogWarning(
                    ex,
                    "Failed to list Stripe invoices for customer {CustomerId}",
                    request.StripeCustomerId
                );
            }
        }

        var subscriptionStatus = subscription?.Status ?? request.PaymentStatus;
        var billingAnchorDate = GetSubscriptionPeriodEnd(subscription) ?? request.NextBillingDate;
        var nextPaymentDate = IsCanceledStatus(subscriptionStatus) ? null : billingAnchorDate;
        var hasRecurringPayment = !string.IsNullOrWhiteSpace(
            subscription?.Id ?? request.StripeSubscriptionId
        );
        DateTime? gracePeriodEndsAt =
            subscriptionStatus == "past_due" && nextPaymentDate.HasValue
                ? nextPaymentDate.Value.AddDays(7)
                : null;
        var availabilityEndsAt = IsCanceledStatus(subscriptionStatus)
            ? billingAnchorDate
            : gracePeriodEndsAt ?? nextPaymentDate;

        return new BillingOverviewResult(
            SubscriptionStatus: string.IsNullOrWhiteSpace(subscriptionStatus)
                ? request.PaymentStatus
                : subscriptionStatus,
            HasRecurringPayment: hasRecurringPayment,
            NextPaymentDate: nextPaymentDate,
            GracePeriodEndsAt: gracePeriodEndsAt,
            AvailabilityEndsAt: availabilityEndsAt,
            RecentTransactions: invoices?.Data.Select(MapInvoice).ToArray() ?? []
        );
    }

    public async Task<BillingPortalSessionResult> CreateBillingPortalSessionAsync(
        string customerId,
        string returnUrl,
        CancellationToken cancellationToken = default
    )
    {
        if (IsSimulatorEndpoint())
            return new BillingPortalSessionResult(returnUrl);

        var service = new StripeBillingPortalSessionService(CreateStripeClient());
        var session = await service
            .CreateAsync(
                new StripeBillingPortalSessionCreateOptions
                {
                    Customer = customerId,
                    ReturnUrl = returnUrl,
                },
                cancellationToken: cancellationToken
            )
            .ConfigureAwait(false);

        return new BillingPortalSessionResult(session.Url);
    }

    public async Task<BillingSubscriptionUpdateResult> UpdateSubscriptionAsync(
        string subscriptionId,
        string priceId,
        bool prorate,
        CancellationToken cancellationToken = default
    )
    {
        if (IsSimulatorEndpoint())
        {
            return new BillingSubscriptionUpdateResult(
                SubscriptionId: subscriptionId,
                SubscriptionStatus: "active",
                CurrentPeriodEnd: DateTime.UtcNow.AddMonths(1)
            );
        }

        var service = new SubscriptionService(CreateStripeClient());
        var current = await service
            .GetAsync(subscriptionId, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        var currentItem = current.Items.Data.FirstOrDefault();

        if (currentItem is null)
            throw new InvalidOperationException(
                $"Stripe subscription '{subscriptionId}' has no line items to update."
            );

        var updated = await service
            .UpdateAsync(
                subscriptionId,
                new SubscriptionUpdateOptions
                {
                    ProrationBehavior = prorate ? "create_prorations" : "none",
                    Items = [new SubscriptionItemOptions { Id = currentItem.Id, Price = priceId }],
                },
                cancellationToken: cancellationToken
            )
            .ConfigureAwait(false);

        return new BillingSubscriptionUpdateResult(
            SubscriptionId: updated.Id,
            SubscriptionStatus: updated.Status,
            CurrentPeriodEnd: GetSubscriptionPeriodEnd(updated)
        );
    }

    // removed — addon only

    // removed — addon only

    // removed — addon only

    private bool IsSimulatorEndpoint()
    {
        if (!Uri.TryCreate(_options.BaseUrl, UriKind.Absolute, out var uri))
            return false;

        return uri.IsLoopback
            || uri.Host.Contains("payment-simulator", StringComparison.OrdinalIgnoreCase);
    }

    public bool VerifyWebhookSignature(byte[] payload, string signatureHeader, string secret)
    {
        try
        {
            if (!IsSimulatorEndpoint())
            {
                EventUtility.ConstructEvent(
                    Encoding.UTF8.GetString(payload),
                    signatureHeader,
                    secret
                );
                return true;
            }

            var parts = signatureHeader.Split(',');
            string? timestamp = null;
            string? v1Signature = null;

            foreach (var part in parts)
            {
                var kv = part.Split('=', 2);
                if (kv.Length != 2)
                    continue;
                if (kv[0] == "t")
                    timestamp = kv[1];
                if (kv[0] == "v1")
                    v1Signature = kv[1];
            }

            if (timestamp is null || v1Signature is null)
                return false;

            var signedPayload = $"{timestamp}.{Encoding.UTF8.GetString(payload)}";
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signedPayload));
            var computedSignature = Convert.ToHexStringLower(computedHash);

            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(computedSignature),
                Encoding.UTF8.GetBytes(v1Signature)
            );
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to verify webhook signature");
            return false;
        }
    }

    [SuppressMessage(
        "Maintainability",
        "S3776:Cognitive Complexity of methods should not be too high",
        Justification = "Webhook payload parsing needs explicit field extraction across multiple Stripe event shapes."
    )]
    public PaymentWebhookEvent ParseWebhookEvent(byte[] payload)
    {
        var json = JsonDocument.Parse(payload);
        var root = json.RootElement;
        var data = root.GetProperty("data").GetProperty("object");

        Dictionary<string, string>? metadata = null;
        if (
            data.TryGetProperty("metadata", out var metaElement)
            && metaElement.ValueKind == JsonValueKind.Object
        )
        {
            metadata = [];
            foreach (var prop in metaElement.EnumerateObject())
                metadata[prop.Name] = prop.Value.GetString() ?? string.Empty;
        }

        var currentPeriodEnd =
            ReadUnixTimestamp(data, "current_period_end")
            ?? ReadUnixTimestamp(data, "period_end")
            ?? ReadNestedPeriodEnd(data);

        var nextPaymentAttemptAt = ReadUnixTimestamp(data, "next_payment_attempt");
        var subscriptionStatus = string.Empty;
        if (data.TryGetProperty("subscription_status", out var explicitSubscriptionStatus))
            subscriptionStatus = explicitSubscriptionStatus.GetString() ?? string.Empty;
        else if (data.TryGetProperty("status", out var nestedStatus))
            subscriptionStatus = nestedStatus.GetString() ?? string.Empty;

        return new PaymentWebhookEvent(
            EventId: root.GetProperty("id").GetString()!,
            EventType: root.GetProperty("type").GetString()!,
            SessionId: data.GetProperty("id").GetString()!,
            PaymentIntentId: data.TryGetProperty("payment_intent", out var pi)
                ? pi.GetString() ?? ""
                : "",
            SubscriptionId: data.TryGetProperty("subscription", out var sub)
                ? sub.GetString() ?? ""
                : "",
            Status: data.TryGetProperty("status", out var st) ? st.GetString() ?? "" : "",
            AmountTotal: data.TryGetProperty("amount_total", out var amt) ? amt.GetInt64() : 0,
            Currency: data.TryGetProperty("currency", out var cur) ? cur.GetString() ?? "" : "",
            Metadata: metadata,
            CustomerId: data.TryGetProperty("customer", out var customer)
                ? customer.GetString() ?? ""
                : "",
            InvoiceId: data.TryGetProperty("invoice", out var invoice)
                ? invoice.GetString() ?? ""
                : "",
            SubscriptionStatus: subscriptionStatus,
            BillingReason: data.TryGetProperty("billing_reason", out var billingReason)
                ? billingReason.GetString() ?? ""
                : "",
            CurrentPeriodEnd: currentPeriodEnd,
            NextPaymentAttemptAt: nextPaymentAttemptAt,
            HostedInvoiceUrl: data.TryGetProperty("hosted_invoice_url", out var hostedInvoiceUrl)
                ? hostedInvoiceUrl.GetString() ?? ""
                : "",
            InvoicePdfUrl: data.TryGetProperty("invoice_pdf", out var invoicePdfUrl)
                ? invoicePdfUrl.GetString() ?? ""
                : "",
            Description: data.TryGetProperty("description", out var description)
                ? description.GetString() ?? ""
                : ""
        );
    }

    [SuppressMessage(
        "Maintainability",
        "S107:Methods should not have too many parameters",
        Justification = "Simulator checkout arguments are grouped in a dedicated request record."
    )]
    private async Task<CheckoutSessionResult> CreateSimulatorCheckoutAsync(
        SimulatorCheckoutRequest request,
        CancellationToken cancellationToken
    )
    {
        var client = httpClientFactory.CreateClient("Stripe");

        var requestBody = new
        {
            price_id = request.Metadata.GetValueOrDefault(PlanIdMetadataKey, string.Empty),
            amount = request.Amount,
            currency = request.Currency,
            mode = request.Mode,
            success_url = request.SuccessUrl,
            cancel_url = request.CancelUrl,
            customer_id = request.CustomerId,
            metadata = request.Metadata,
        };

        var response = await client
            .PostAsJsonAsync("/v1/checkout/sessions", requestBody, cancellationToken)
            .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        var session = await response
            .Content.ReadFromJsonAsync<JsonElement>(cancellationToken)
            .ConfigureAwait(false);

        return new CheckoutSessionResult(
            SessionId: session.GetProperty("id").GetString()!,
            SessionUrl: session.GetProperty("url").GetString()!,
            PaymentIntentId: session.TryGetProperty("payment_intent", out var paymentIntent)
                ? paymentIntent.GetString() ?? string.Empty
                : string.Empty,
            CustomerId: session.TryGetProperty("customer", out var customer)
                ? customer.GetString() ?? string.Empty
                : request.CustomerId ?? string.Empty,
            SubscriptionId: session.TryGetProperty("subscription", out var subscription)
                ? subscription.GetString() ?? string.Empty
                : string.Empty
        );
    }

    private static List<SessionLineItemOptions> CreateSubscriptionLineItems(
        RecurringSubscriptionCheckoutRequest request,
        Dictionary<string, string> metadata
    )
    {
        var lineItems = new List<SessionLineItemOptions>
        {
            CreateSubscriptionLineItem(request, metadata),
        };

        if (request.OneTimeAmount.GetValueOrDefault() > 0)
        {
            lineItems.Add(
                new SessionLineItemOptions
                {
                    Quantity = 1,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = request.Currency.ToLowerInvariant(),
                        UnitAmount = request.OneTimeAmount,
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = string.IsNullOrWhiteSpace(request.OneTimeDescription)
                                ? $"One-time charge for {request.PlanName}"
                                : request.OneTimeDescription,
                            Metadata = metadata,
                        },
                    },
                }
            );
        }

        return lineItems;
    }

    private static SessionLineItemOptions CreateSubscriptionLineItem(
        RecurringSubscriptionCheckoutRequest request,
        Dictionary<string, string> metadata
    )
    {
        var lineItem = new SessionLineItemOptions { Quantity = 1 };

        if (!string.IsNullOrWhiteSpace(request.StripePriceId))
        {
            lineItem.Price = request.StripePriceId;
            return lineItem;
        }

        lineItem.PriceData = new SessionLineItemPriceDataOptions
        {
            Currency = request.Currency.ToLowerInvariant(),
            UnitAmount = request.Amount,
            Recurring = new SessionLineItemPriceDataRecurringOptions { Interval = MonthlyInterval },
            ProductData = new SessionLineItemPriceDataProductDataOptions
            {
                Name = request.PlanName,
                Metadata = metadata,
            },
        };

        return lineItem;
    }

    private static BillingOverviewResult BuildFallbackOverview(BillingOverviewRequest request)
    {
        var subscriptionStatus = string.IsNullOrWhiteSpace(request.PaymentStatus)
            ? "none"
            : request.PaymentStatus;
        var billingAnchorDate = request.NextBillingDate;
        var nextPaymentDate = IsCanceledStatus(subscriptionStatus) ? null : billingAnchorDate;
        DateTime? gracePeriodEndsAt =
            subscriptionStatus == "past_due" && nextPaymentDate.HasValue
                ? nextPaymentDate.Value.AddDays(7)
                : null;
        var availabilityEndsAt = IsCanceledStatus(subscriptionStatus)
            ? billingAnchorDate
            : gracePeriodEndsAt ?? nextPaymentDate;

        var recentTransactions = request.LastPaymentDate.HasValue
            ? new[]
            {
                new BillingTransactionResult(
                    Id: $"local-{request.TenantId:N}",
                    Type: "invoice",
                    Status: request.PaymentStatus,
                    Amount: 0,
                    Currency: "brl",
                    OccurredAt: request.LastPaymentDate.Value,
                    Description: "Local billing history"
                ),
            }
            : Array.Empty<BillingTransactionResult>();

        return new BillingOverviewResult(
            SubscriptionStatus: subscriptionStatus,
            HasRecurringPayment: !string.IsNullOrWhiteSpace(request.StripeSubscriptionId),
            NextPaymentDate: nextPaymentDate,
            GracePeriodEndsAt: gracePeriodEndsAt,
            AvailabilityEndsAt: availabilityEndsAt,
            RecentTransactions: recentTransactions
        );
    }

    private static BillingTransactionResult MapInvoice(Invoice invoice) =>
        new(
            Id: invoice.Id,
            Type: "invoice",
            Status: invoice.Status ?? "unknown",
            Amount: invoice.Total,
            Currency: invoice.Currency ?? "brl",
            OccurredAt: invoice.Created,
            Description: string.IsNullOrWhiteSpace(invoice.Description)
                ? invoice.Number ?? invoice.Id
                : invoice.Description,
            HostedInvoiceUrl: invoice.HostedInvoiceUrl
        );

    private static bool IsCanceledStatus(string? subscriptionStatus) =>
        string.Equals(subscriptionStatus, "canceled", StringComparison.OrdinalIgnoreCase);

    private static DateTime? ReadUnixTimestamp(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
            return null;

        return property.ValueKind switch
        {
            JsonValueKind.Number when property.TryGetInt64(out var unix) => DateTimeOffset
                .FromUnixTimeSeconds(unix)
                .UtcDateTime,
            JsonValueKind.String when long.TryParse(property.GetString(), out var unix) =>
                DateTimeOffset.FromUnixTimeSeconds(unix).UtcDateTime,
            _ => null,
        };
    }

    private static DateTime? ReadNestedPeriodEnd(JsonElement data)
    {
        if (!data.TryGetProperty("lines", out var lines) || lines.ValueKind != JsonValueKind.Object)
            return null;

        if (
            !lines.TryGetProperty("data", out var lineData)
            || lineData.ValueKind != JsonValueKind.Array
        )
            return null;

        foreach (var line in lineData.EnumerateArray())
        {
            if (
                !line.TryGetProperty("period", out var period)
                || period.ValueKind != JsonValueKind.Object
            )
                continue;

            var periodEnd = ReadUnixTimestamp(period, "end");
            if (periodEnd.HasValue)
                return periodEnd;
        }

        return null;
    }

    private static DateTime? GetSubscriptionPeriodEnd(Subscription? subscription) =>
        subscription?.Items?.Data?.FirstOrDefault()?.CurrentPeriodEnd;

    private static Dictionary<string, string> BuildPlanMetadata(Guid planId) =>
        new() { [PlanIdMetadataKey] = planId.ToString(), ["managed_by"] = "homeless-api" };

    // removed — addon only

    private static bool MatchesMonthlyCatalog(Price price, string currency, long unitAmount) =>
        price.Active
        && price.UnitAmount == unitAmount
        && string.Equals(price.Currency, currency, StringComparison.OrdinalIgnoreCase)
        && string.Equals(
            price.Recurring?.Interval,
            MonthlyInterval,
            StringComparison.OrdinalIgnoreCase
        )
        && (price.Recurring?.IntervalCount ?? 1) == 1;

    private static async Task<Product?> TryRecoverPlanProductAsync(
        ProductService productService,
        Guid planId,
        CancellationToken cancellationToken
    )
    {
        var products = await productService
            .ListAsync(
                new ProductListOptions { Active = true, Limit = 100 },
                cancellationToken: cancellationToken
            )
            .ConfigureAwait(false);

        return StripePlanCatalogRecovery.FindManagedPlanProduct(planId, products.Data);
    }

    // removed — addon only

    // removed — addon only

    private static long ToMinorUnits(decimal amount) =>
        checked((long)decimal.Round(amount * 100m, 0, MidpointRounding.AwayFromZero));

    private static PlanStripeCatalogResult BuildSimulatorPlanCatalog(
        PlanStripeCatalogRequest request
    )
    {
        var priceId = string.IsNullOrWhiteSpace(request.StripePriceId)
            ? $"price_sim_{request.PlanId:N}"
            : request.StripePriceId.Trim();

        return new PlanStripeCatalogResult(
            StripePriceId: priceId,
            StripeProductId: $"prod_sim_{request.PlanId:N}",
            CreatedProduct: string.IsNullOrWhiteSpace(request.StripePriceId),
            CreatedPrice: string.IsNullOrWhiteSpace(request.StripePriceId)
        );
    }

    // removed — addon only

    private sealed record SimulatorCheckoutRequest(
        string Mode,
        long Amount,
        string Currency,
        string SuccessUrl,
        string CancelUrl,
        Dictionary<string, string> Metadata,
        string? CustomerId
    );

    // removed — addon only
}
