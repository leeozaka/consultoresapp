using Ardalis.Result;
using MediatR;
using Homeless.Application.DTOs;

namespace Homeless.Application.UseCases.Webhooks;

/// <summary>
/// Dispatches a parsed, signature-verified Stripe webhook event to the appropriate
/// business logic flow. The controller is responsible for signature verification and
/// idempotency; this handler owns all domain state changes.
/// </summary>
public sealed record ProcessStripeWebhookCommand(PaymentWebhookEvent Event) : IRequest<Result>;
