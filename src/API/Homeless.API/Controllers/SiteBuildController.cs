using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Homeless.Application.Authorization;
using Homeless.Application.DTOs;
using Homeless.Application.Interfaces;

namespace Homeless.API.Controllers;

[ApiController]
[Route("api/admin/site-builds")]
[Authorize(Roles = Roles.SuperAdmin)]
public sealed class SiteBuildController(ISiteBuildStatusStream statusStream) : ControllerBase
{
    [HttpGet("{tenantId:guid}/events")]
    [Produces("text/event-stream")]
    public async Task Stream(Guid tenantId, CancellationToken cancellationToken)
    {
        var stream = statusStream.ReadAllAsync(tenantId, cancellationToken);
        await TypedResults.ServerSentEvents(stream, eventType: "site-build-status").ExecuteAsync(HttpContext);
    }
}
