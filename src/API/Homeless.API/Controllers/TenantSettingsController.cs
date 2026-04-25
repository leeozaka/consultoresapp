using Ardalis.Result;
using Ardalis.Result.AspNetCore;
using Homeless.Application.Authorization;
using Homeless.Application.DTOs;
using Homeless.Application.Interfaces;
using Homeless.Application.UseCases.Tenants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Homeless.API.Controllers;

/// <summary>
/// Tenant-admin self-service endpoints for viewing and editing own tenant settings.
/// TenantId is resolved from ITenantContext (JWT), never from the URL.
/// </summary>
[ApiController]
[Route("api/tenant/settings")]
[Produces("application/json")]
[Authorize(Roles = Roles.TenantAdmin)]
[TranslateResultToActionResult]
public sealed class TenantSettingsController(IMediator mediator, ITenantContext tenantContext)
    : ControllerBase
{
    /// <summary>
    /// Returns the current tenant's settings (same shape as TenantResponse).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(TenantResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ExpectedFailures(ResultStatus.NotFound)]
    public async Task<Result<TenantResponse>> GetSettings(CancellationToken cancellationToken)
    {
        if (!tenantContext.IsResolved)
            return Result.NotFound("No tenant resolved for this request.");

        return await mediator.Send(
            new GetTenantByIdQuery(tenantContext.TenantId),
            cancellationToken
        );
    }

    /// <summary>
    /// Updates the current tenant's editable settings (name, contact email, phone).
    /// </summary>
    [HttpPut]
    [ProducesResponseType(typeof(TenantResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ExpectedFailures(ResultStatus.NotFound, ResultStatus.Invalid, ResultStatus.Forbidden)]
    public async Task<Result<TenantResponse>> UpdateSettings(
        [FromBody] UpdateTenantSettingsRequest request,
        CancellationToken cancellationToken
    ) =>
        await mediator.Send(
            new UpdateTenantSettingsCommand(
                request.Name,
                request.ContactEmail,
                request.ContactPhone
            ),
            cancellationToken
        );
}
