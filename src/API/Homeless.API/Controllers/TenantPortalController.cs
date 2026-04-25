using Ardalis.Result;
using Ardalis.Result.AspNetCore;
using Homeless.Application.DTOs;
using Homeless.Application.Interfaces;
using Homeless.Application.Options;
using Homeless.Application.UseCases.Tenants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Homeless.API.Controllers;

/// <summary>
/// Public tenant portal endpoints — no authentication required.
/// Used by the Angular frontend to bootstrap tenant context and branding.
/// </summary>
[ApiController]
[Route("api/tenants")]
[Produces("application/json")]
[AllowAnonymous]
[TranslateResultToActionResult]
public sealed class TenantPortalController(
    IMediator mediator,
    ITenantContext tenantContext,
    IOptions<AppOptions> appOptions
) : ControllerBase
{
    /// <summary>
    /// Returns publicly-visible tenant information (including branding) for the given slug.
    /// Called once on portal load to hydrate the tenant context in the frontend.
    /// </summary>
    [HttpGet("by-slug/{slug}")]
    [ProducesResponseType(typeof(TenantResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ExpectedFailures(ResultStatus.NotFound)]
    public async Task<Result<TenantResponse>> GetBySlug(
        [FromRoute] string slug,
        CancellationToken cancellationToken
    ) => await mediator.Send(new GetTenantBySlugQuery(slug), cancellationToken);

    /// <summary>
    /// Resolves tenant information by the current request host (custom domain or subdomain).
    /// </summary>
    [HttpGet("resolve")]
    [ProducesResponseType(typeof(TenantResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ExpectedFailures(ResultStatus.NotFound)]
    public async Task<Result<TenantResponse>> ResolveByHost(CancellationToken cancellationToken)
    {
        var host = HttpContext.Request.Host.Host;
        return await mediator.Send(
            new GetTenantByHostQuery(
                host,
                appOptions.Value.LandingDomain,
                appOptions.Value.LandingTenantSlug
            ),
            cancellationToken
        );
    }

    /// <summary>
    /// Returns the tenant information for the currently authenticated user.
    /// TenantResolutionMiddleware resolves the tenant from the JWT Bearer token
    /// (JWT fallback on root domain) and populates ITenantContext before this runs.
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(TenantResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ExpectedFailures(ResultStatus.NotFound)]
    public async Task<Result<TenantResponse>> GetMe(CancellationToken cancellationToken)
    {
        if (!tenantContext.IsResolved)
            return Result.NotFound("No tenant resolved for this request.");

        return await mediator.Send(
            new GetTenantByIdQuery(tenantContext.TenantId),
            cancellationToken
        );
    }
}
