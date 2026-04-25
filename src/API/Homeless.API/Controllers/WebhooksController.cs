using Homeless.Application.DTOs;
using Homeless.Application.Interfaces;
using Homeless.Application.Options;
using Homeless.Application.UseCases.Webhooks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Homeless.API.Controllers;

[ApiController]
[Route("api/webhooks")]
public sealed class WebhooksController(
    IMediator mediator,
    IPaymentGateway paymentGateway,
    ICacheService cacheService,
    IOptions<StripeOptions> stripeOptions,
    ILogger<WebhooksController> logger
) : ControllerBase
{
    private readonly StripeOptions _stripeOptions = stripeOptions.Value;

    [HttpPost("stripe")]
    public async Task<IActionResult> StripeWebhook(
        [FromHeader(Name = "Stripe-Signature")] string signatureHeader,
        CancellationToken cancellationToken
    )
    {
        var payload = await ReadBodyAsync().ConfigureAwait(false);

        if (
            !paymentGateway.VerifyWebhookSignature(
                payload,
                signatureHeader,
                _stripeOptions.WebhookSecret
            )
        )
        {
            logger.LogWarning("Invalid Stripe webhook signature");
            return Unauthorized();
        }

        PaymentWebhookEvent webhookEvent;
        try
        {
            webhookEvent = paymentGateway.ParseWebhookEvent(payload);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to parse Stripe webhook payload");
            return BadRequest();
        }

        // Idempotency — skip completed events, but only mark after successful handling
        // so Stripe retries can recover transient failures.
        var eventKey = CacheKeys.WebhookEvent(webhookEvent.EventId);
        var alreadyProcessed = await cacheService
            .GetAsync<string>(eventKey, cancellationToken)
            .ConfigureAwait(false);

        if (alreadyProcessed is not null)
        {
            logger.LogInformation(
                "Duplicate webhook event {EventId}, skipping",
                webhookEvent.EventId
            );
            return Ok();
        }

        await mediator
            .Send(new ProcessStripeWebhookCommand(webhookEvent), cancellationToken)
            .ConfigureAwait(false);

        await cacheService
            .SetAsync(eventKey, "processed", TimeSpan.FromHours(24), cancellationToken)
            .ConfigureAwait(false);

        return Ok();
    }

    private async Task<byte[]> ReadBodyAsync()
    {
        using var ms = new MemoryStream();
        await Request.Body.CopyToAsync(ms).ConfigureAwait(false);
        return ms.ToArray();
    }
}
