using Ardalis.Result;
using Ardalis.Result.AspNetCore;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Homeless.Application.DTOs;
using Homeless.Application.Interfaces;
using Homeless.Application.UseCases.Onboarding;
using Homeless.Application.UseCases.Plans;

namespace Homeless.API.Controllers;

/// <summary>
/// Self-service onboarding endpoints — anonymous, rate-limited.
/// Covers the signup wizard, slug availability, plans listing, and post-payment status.
/// </summary>
[ApiController]
[Route("api/onboarding")]
[Produces("application/json")]
[AllowAnonymous]
[TranslateResultToActionResult]
public sealed class OnboardingController(
    IMediator mediator,
    IOnboardingStatusStream onboardingStatusStream) : ControllerBase
{
    /// <summary>
    /// Self-service signup: creates tenant + user + initiates Stripe checkout in one operation.
    /// Returns a checkout URL the client must redirect the user to.
    /// </summary>
    [HttpPost("signup")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(SignupResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ExpectedFailures(ResultStatus.Invalid, ResultStatus.Conflict, ResultStatus.NotFound, ResultStatus.Error)]
    public async Task<Result<SignupResponse>> Signup(
        [FromBody] SignupRequest request,
        CancellationToken cancellationToken) =>
        await mediator.Send(new SignupCommand(
            AgencyName: request.AgencyName,
            Slug: request.Slug,
            ContactEmail: request.ContactEmail,
            ContactPhone: request.ContactPhone,
            PlanId: request.PlanId,
            FirstName: request.FirstName,
            LastName: request.LastName,
            Email: request.Email,
            Password: request.Password,
            SuccessUrl: request.SuccessUrl,
            CancelUrl: request.CancelUrl),
            cancellationToken);

    /// <summary>
    /// Real-time slug availability check for the signup wizard.
    /// Returns <c>{ "slug": "...", "available": true/false }</c>.
    /// </summary>
    [HttpGet("check-slug/{slug}")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(SlugAvailabilityResponse), StatusCodes.Status200OK)]
    public async Task<Result<SlugAvailabilityResponse>> CheckSlug(
        string slug,
        CancellationToken cancellationToken) =>
        await mediator.Send(new CheckSlugQuery(slug), cancellationToken);

    /// <summary>
    /// Lists all active subscription plans for the plan selection step.
    /// </summary>
    [HttpGet("plans")]
    [ProducesResponseType(typeof(PaginatedResponse<PlanResponse>), StatusCodes.Status200OK)]
    public async Task<PaginatedResponse<PlanResponse>> GetPlans(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetPlansQuery(ActiveOnly: true), cancellationToken);
        return PaginatedResponse<PlanResponse>.CreateFrom(result.Value, page, pageSize);
    }

    /// <summary>
    /// Returns wizard data for a Pending tenant so the frontend can resume an interrupted signup.
    /// </summary>
    [HttpGet("{tenantId:guid}/resume")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(SignupResumeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ExpectedFailures(ResultStatus.NotFound)]
    public async Task<Result<SignupResumeResponse>> GetResume(
        Guid tenantId,
        CancellationToken cancellationToken) =>
        await mediator.Send(new GetSignupResumeQuery(tenantId), cancellationToken);

    /// <summary>
    /// Explicitly dismisses an incomplete signup, freeing the held slug and email.
    /// Requires the dismiss token returned during signup.
    /// </summary>
    [HttpPost("{tenantId:guid}/dismiss")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ExpectedFailures(ResultStatus.NotFound, ResultStatus.Conflict, ResultStatus.Unauthorized)]
    public async Task<Result> Dismiss(
        Guid tenantId,
        [FromBody] DismissSignupRequest request,
        CancellationToken cancellationToken) =>
        await mediator.Send(new DismissSignupCommand(tenantId, request.DismissToken), cancellationToken);

    /// <summary>
    /// Returns the current onboarding state for a tenant.
    /// Used for polling on the /onboarding/status/:tenantId page.
    /// </summary>
    [HttpGet("{tenantId:guid}/status")]
    [ProducesResponseType(typeof(OnboardingStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ExpectedFailures(ResultStatus.NotFound)]
    public async Task<Result<OnboardingStatusResponse>> GetStatus(
        Guid tenantId,
        CancellationToken cancellationToken) =>
        await mediator.Send(new GetOnboardingStatusQuery(tenantId), cancellationToken);

    /// <summary>
    /// SSE stream for real-time onboarding progress updates.
    /// Connect after checkout redirect to receive provisioning status events.
    /// </summary>
    [HttpGet("{tenantId:guid}/status/events")]
    [Produces("text/event-stream")]
    public async Task StreamStatus(Guid tenantId, CancellationToken cancellationToken)
    {
        var stream = onboardingStatusStream.ReadForTenantAsync(tenantId, cancellationToken);

        try
        {
            await TypedResults
                .ServerSentEvents(stream, eventType: "onboarding-status")
                .ExecuteAsync(HttpContext);
        }
        catch (OperationCanceledException) when (
            cancellationToken.IsCancellationRequested ||
            HttpContext.RequestAborted.IsCancellationRequested)
        {
            // Client disconnected.
        }
    }
}
