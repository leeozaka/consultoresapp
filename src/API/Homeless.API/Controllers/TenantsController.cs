using Ardalis.Result;
using Ardalis.Result.AspNetCore;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Homeless.Application.Authorization;
using Homeless.Application.DTOs;
using Homeless.Application.UseCases.Tenants;

namespace Homeless.API.Controllers;

/// <summary>
/// SuperAdmin-only tenant management endpoints. Bypasses tenant resolution middleware.
/// </summary>
[ApiController]
[Route("api/admin/tenants")]
[Produces("application/json")]
[Authorize(Roles = Roles.SuperAdmin)]
[TranslateResultToActionResult]
public sealed class TenantsController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Lists all tenants.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(CursorPaginatedResponse<TenantResponse>), StatusCodes.Status200OK)]
    public async Task<CursorPaginatedResponse<TenantResponse>> GetAll(
        [FromQuery] int pageSize = 50,
        [FromQuery] string? after = null,
        CancellationToken cancellationToken = default) =>
        (await mediator.Send(new GetAllTenantsQuery(pageSize, after), cancellationToken)).Value;

    /// <summary>
    /// Gets a single tenant by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TenantResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ExpectedFailures(ResultStatus.NotFound)]
    public async Task<Result<TenantResponse>> GetById(Guid id, CancellationToken cancellationToken) =>
        await mediator.Send(new GetTenantByIdQuery(id), cancellationToken);

    /// <summary>
    /// Lists only pending tenants waiting for SuperAdmin review.
    /// </summary>
    [HttpGet("pending")]
    [ProducesResponseType(typeof(PaginatedResponse<TenantResponse>), StatusCodes.Status200OK)]
    public async Task<PaginatedResponse<TenantResponse>> GetPending(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetPendingTenantsQuery(), cancellationToken);
        return PaginatedResponse<TenantResponse>.CreateFrom(result.Value, page, pageSize);
    }

    /// <summary>
    /// Creates a new tenant (agency).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(TenantResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ExpectedFailures(ResultStatus.Invalid)]
    public async Task<Result<TenantResponse>> Create(
        [FromBody] CreateTenantRequest request,
        CancellationToken cancellationToken) =>
        await mediator.Send(new CreateTenantCommand(
            request.Name, request.Slug, request.ContactEmail,
            request.ContactPhone, request.CustomDomain, request.NextBillingDate), cancellationToken);

    /// <summary>
    /// Activates a pending or suspended tenant.
    /// </summary>
    [HttpPost("{id:guid}/activate")]
    [ProducesResponseType(typeof(TenantResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ExpectedFailures(ResultStatus.NotFound)]
    public async Task<Result<TenantResponse>> Activate(Guid id, CancellationToken cancellationToken) =>
        await mediator.Send(new ActivateTenantCommand(id), cancellationToken);

    /// <summary>
    /// Suspends an active or pending tenant, disabling access for its users.
    /// </summary>
    [HttpPost("{id:guid}/suspend")]
    [ProducesResponseType(typeof(TenantResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ExpectedFailures(ResultStatus.NotFound)]
    public async Task<Result<TenantResponse>> Suspend(
        Guid id,
        [FromQuery] string? reason,
        CancellationToken cancellationToken) =>
        await mediator.Send(new SuspendTenantCommand(id, reason), cancellationToken);

    /// <summary>
    /// Approves a pending tenant, activates it and triggers async site build provisioning.
    /// </summary>
    [HttpPost("{id:guid}/approve")]
    [ProducesResponseType(typeof(TenantResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ExpectedFailures(ResultStatus.NotFound)]
    public async Task<Result<TenantResponse>> Approve(Guid id, CancellationToken cancellationToken) =>
        await mediator.Send(new ApproveTenantCommand(id), cancellationToken);

    /// <summary>
    /// Updates a tenant's branding configuration.
    /// </summary>
    [HttpPut("{id:guid}/branding")]
    [ProducesResponseType(typeof(TenantResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ExpectedFailures(ResultStatus.NotFound)]
    public async Task<Result<TenantResponse>> UpdateBranding(
        Guid id,
        [FromBody] UpdateTenantBrandingRequest request,
        CancellationToken cancellationToken) =>
        await mediator.Send(new UpdateTenantBrandingCommand(id, request), cancellationToken);

    /// <summary>
    /// Sets or clears a tenant custom domain.
    /// </summary>
    [HttpPut("{id:guid}/domain")]
    [ProducesResponseType(typeof(TenantResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ExpectedFailures(ResultStatus.NotFound, ResultStatus.Conflict, ResultStatus.Forbidden)]
    public async Task<Result<TenantResponse>> SetCustomDomain(
        Guid id,
        [FromBody] SetTenantCustomDomainRequest request,
        CancellationToken cancellationToken) =>
        await mediator.Send(new SetTenantCustomDomainCommand(id, request.CustomDomain), cancellationToken);

    /// <summary>
    /// Sets or clears a tenant custom frontend origin.
    /// </summary>
    [HttpPut("{id:guid}/frontend-origin")]
    [ProducesResponseType(typeof(TenantResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ExpectedFailures(ResultStatus.NotFound, ResultStatus.Forbidden)]
    public async Task<Result<TenantResponse>> SetFrontendOrigin(
        Guid id,
        [FromBody] SetTenantFrontendOriginRequest request,
        CancellationToken cancellationToken) =>
        await mediator.Send(new SetTenantFrontendOriginCommand(id, request.FrontendOrigin), cancellationToken);

    /// <summary>
    /// Updates a tenant's details (name, slug, contact info, domain, billing).
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(TenantResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ExpectedFailures(ResultStatus.NotFound, ResultStatus.Invalid, ResultStatus.Conflict)]
    public async Task<Result<TenantResponse>> Update(
        Guid id,
        [FromBody] UpdateTenantRequest request,
        CancellationToken cancellationToken) =>
        await mediator.Send(new UpdateTenantCommand(
            id, request.Name, request.Slug, request.ContactEmail,
            request.ContactPhone, request.CustomDomain, request.NextBillingDate), cancellationToken);

    /// <summary>
    /// Directly overrides a tenant's entitlements, bypassing plan constraints.
    /// Cache is invalidated immediately.
    /// </summary>
    [HttpPut("{id:guid}/entitlements")]
    [ProducesResponseType(typeof(TenantResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ExpectedFailures(ResultStatus.NotFound)]
    public async Task<Result<TenantResponse>> UpdateEntitlements(
        Guid id,
        [FromBody] UpdateTenantEntitlementsRequest request,
        CancellationToken cancellationToken) =>
        await mediator.Send(new UpdateTenantEntitlementsCommand(id, request.Entitlements), cancellationToken);

    /// <summary>
    /// Assigns a subscription plan to a tenant, merging the plan's base entitlements.
    /// This is a SuperAdmin override and does not trigger any billing flow.
    /// </summary>
    [HttpPut("{id:guid}/plan")]
    [ProducesResponseType(typeof(TenantResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ExpectedFailures(ResultStatus.NotFound)]
    public async Task<Result<TenantResponse>> AssignPlan(
        Guid id,
        [FromBody] AssignTenantPlanRequest request,
        CancellationToken cancellationToken) =>
        await mediator.Send(new AssignTenantPlanCommand(id, request.PlanId), cancellationToken);

    /// <summary>
    /// Archives a tenant, permanently removing portal access.
    /// </summary>
    [HttpPost("{id:guid}/archive")]
    [ProducesResponseType(typeof(TenantResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ExpectedFailures(ResultStatus.NotFound)]
    public async Task<Result<TenantResponse>> Archive(Guid id, CancellationToken cancellationToken) =>
        await mediator.Send(new ArchiveTenantCommand(id), cancellationToken);
}
