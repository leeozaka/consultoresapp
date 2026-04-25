using Ardalis.Result;
using Ardalis.Result.AspNetCore;
using Homeless.Application.Authorization;
using Homeless.Application.DTOs;
using Homeless.Application.UseCases.Tenants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Homeless.API.Controllers;

[ApiController]
[Route("api/tenant/branding")]
[Produces("application/json")]
[Authorize(Roles = Roles.TenantAdmin)]
[TranslateResultToActionResult]
public sealed class TenantBrandingController(IMediator mediator) : ControllerBase
{
    [HttpPut]
    [ProducesResponseType(typeof(TenantResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ExpectedFailures(ResultStatus.NotFound, ResultStatus.Invalid, ResultStatus.Forbidden)]
    public async Task<Result<TenantResponse>> UpdateBranding(
        [FromBody] UpdateTenantBrandingRequest request,
        CancellationToken cancellationToken
    ) => await mediator.Send(new UpdateCurrentTenantBrandingCommand(request), cancellationToken);

    /// <summary>
    /// Uploads a portal marketing image (hero, etc.) to tenant-scoped storage. Not available for custom-site tenants.
    /// </summary>
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(PortalAssetUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ExpectedFailures(ResultStatus.Invalid, ResultStatus.Forbidden, ResultStatus.NotFound)]
    public async Task<Result<PortalAssetUploadResponse>> UploadPortalImage(
        IFormFile file,
        CancellationToken cancellationToken
    )
    {
        await using var stream = file.OpenReadStream();
        return await mediator.Send(
            new UploadTenantPortalImageCommand(
                stream,
                file.FileName,
                file.ContentType,
                file.Length
            ),
            cancellationToken
        );
    }
}
