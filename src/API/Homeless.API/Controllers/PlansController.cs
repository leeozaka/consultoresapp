using Ardalis.Result;
using Ardalis.Result.AspNetCore;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Homeless.Application.Authorization;
using Homeless.Application.DTOs;
using Homeless.Application.UseCases.Plans;

namespace Homeless.API.Controllers;

/// <summary>
/// Subscription plan management.
///
/// Public (authenticated) routes: browse available plans.
/// SuperAdmin routes: full CRUD.
/// TenantAdmin routes: switch own plan (billing stubs in place).
/// </summary>
[ApiController]
[Route("")]
[Produces("application/json")]
[TranslateResultToActionResult]
public sealed class PlansController(IMediator mediator) : ControllerBase
{
    /// <summary>Lists all active subscription plans. Public — used by the landing page pricing section.</summary>
    [HttpGet("api/plans")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PaginatedResponse<PlanResponse>), StatusCodes.Status200OK)]
    public async Task<PaginatedResponse<PlanResponse>> GetPlans(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetPlansQuery(ActiveOnly: true), cancellationToken);
        return PaginatedResponse<PlanResponse>.CreateFrom(result.Value, page, pageSize);
    }

    /// <summary>Lists all plans (including inactive). SuperAdmin only.</summary>
    [HttpGet("api/admin/plans")]
    [Authorize(Roles = Roles.SuperAdmin)]
    [ProducesResponseType(typeof(PaginatedResponse<PlanResponse>), StatusCodes.Status200OK)]
    public async Task<PaginatedResponse<PlanResponse>> GetAllPlans(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetPlansQuery(ActiveOnly: false), cancellationToken);
        return PaginatedResponse<PlanResponse>.CreateFrom(result.Value, page, pageSize);
    }

    /// <summary>Creates a new subscription plan.</summary>
    [HttpPost("api/admin/plans")]
    [Authorize(Roles = Roles.SuperAdmin)]
    [ProducesResponseType(typeof(PlanResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ExpectedFailures(ResultStatus.Invalid)]
    public async Task<Result<PlanResponse>> CreatePlan(
        [FromBody] CreatePlanRequest request,
        CancellationToken cancellationToken) =>
        await mediator.Send(
            new CreatePlanCommand(
                request.Name, request.Description, request.PricePerMonth,
                request.MaxProperties, request.VideoUpload, request.AiDescriptions,
                request.CustomDomain, request.PremiumAnalytics, request.PortalTheme, request.StripePriceId),
            cancellationToken);

    /// <summary>Updates an existing plan.</summary>
    [HttpPut("api/admin/plans/{planId:guid}")]
    [Authorize(Roles = Roles.SuperAdmin)]
    [ProducesResponseType(typeof(PlanResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ExpectedFailures(ResultStatus.NotFound, ResultStatus.Invalid)]
    public async Task<Result<PlanResponse>> UpdatePlan(
        Guid planId,
        [FromBody] UpdatePlanRequest request,
        CancellationToken cancellationToken) =>
        await mediator.Send(
            new UpdatePlanCommand(
                planId, request.Name, request.Description, request.PricePerMonth,
                request.MaxProperties, request.VideoUpload, request.AiDescriptions,
                request.CustomDomain, request.PremiumAnalytics, request.PortalTheme, request.StripePriceId),
            cancellationToken);

    /// <summary>Creates or refreshes the Stripe product and price for an existing plan.</summary>
    [HttpPost("api/admin/plans/{planId:guid}/stripe-catalog/sync")]
    [Authorize(Roles = Roles.SuperAdmin)]
    [ProducesResponseType(typeof(PlanResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ExpectedFailures(ResultStatus.NotFound)]
    public async Task<Result<PlanResponse>> SyncStripeCatalog(
        Guid planId,
        CancellationToken cancellationToken) =>
        await mediator.Send(new SyncPlanStripeCatalogCommand(planId), cancellationToken);

    /// <summary>Backfills Stripe catalog mappings for every existing plan.</summary>
    [HttpPost("api/admin/plans/stripe-catalog/backfill")]
    [Authorize(Roles = Roles.SuperAdmin)]
    [ProducesResponseType(typeof(IReadOnlyList<PlanResponse>), StatusCodes.Status200OK)]
    public async Task<Result<IReadOnlyList<PlanResponse>>> BackfillStripeCatalog(
        CancellationToken cancellationToken) =>
        await mediator.Send(new SyncAllPlansStripeCatalogCommand(), cancellationToken);

    /// <summary>Deactivates (soft-deletes) a plan so it is no longer offered to new tenants.</summary>
    [HttpDelete("api/admin/plans/{planId:guid}")]
    [Authorize(Roles = Roles.SuperAdmin)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ExpectedFailures(ResultStatus.NotFound)]
    public async Task<Result> DeactivatePlan(Guid planId, CancellationToken cancellationToken) =>
        await mediator.Send(new DeactivatePlanCommand(planId), cancellationToken);

    /// <summary>
    /// Switches the calling tenant to the specified plan.
    /// Upgrades require a payment gateway (currently stubbed — returns 501).
    /// Downgrades apply immediately; credit is issued at next billing cycle.
    /// </summary>
    [HttpPost("api/tenant/plan")]
    [Authorize(Roles = Roles.TenantAdmin)]
    [ProducesResponseType(typeof(ChangePlanResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ExpectedFailures(ResultStatus.NotFound, ResultStatus.Error)]
    public async Task<Result<ChangePlanResponse>> ChangePlan(
        [FromBody] ChangePlanRequest request,
        CancellationToken cancellationToken) =>
        await mediator.Send(
            new ChangeTenantPlanCommand(request.PlanId, request.SuccessUrl, request.CancelUrl),
            cancellationToken);
}
