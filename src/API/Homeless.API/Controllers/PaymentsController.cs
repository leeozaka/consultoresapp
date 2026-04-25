using System.Runtime.CompilerServices;
using Ardalis.Result;
using Ardalis.Result.AspNetCore;
using Homeless.Application.Authorization;
using Homeless.Application.DTOs;
using Homeless.Application.Interfaces;
using Homeless.Application.UseCases.Payments;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Homeless.API.Controllers;

[ApiController]
[Route("api/admin/payments")]
[Authorize(Roles = Roles.SuperAdmin)]
public sealed class PaymentsController(IPaymentEventStream paymentEventStream) : ControllerBase
{
    [HttpGet("events")]
    [Produces("text/event-stream")]
    public async Task Stream(CancellationToken cancellationToken)
    {
        var stream = paymentEventStream.ReadAllAsync(cancellationToken);

        try
        {
            await TypedResults
                .ServerSentEvents(stream, eventType: "payment-status")
                .ExecuteAsync(HttpContext);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested
                || HttpContext.RequestAborted.IsCancellationRequested
            )
        {
            // Client disconnected.
        }
    }
}

[ApiController]
[Route("api/tenant/payments")]
[Authorize(Roles = $"{Roles.TenantAdmin},{Roles.SuperAdmin}")]
[TranslateResultToActionResult]
public sealed class TenantPaymentsController(
    IPaymentEventStream paymentEventStream,
    ITenantContext tenantContext,
    IMediator mediator
) : ControllerBase
{
    [HttpGet("overview")]
    [ProducesResponseType(typeof(TenantBillingOverviewResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ExpectedFailures(ResultStatus.NotFound)]
    public async Task<Result<TenantBillingOverviewResponse>> GetOverview(
        [FromQuery] int recentTransactions = 10,
        CancellationToken cancellationToken = default
    ) =>
        await mediator.Send(
            new GetTenantBillingOverviewQuery(recentTransactions),
            cancellationToken
        );

    [HttpPost("portal-session")]
    [ProducesResponseType(typeof(BillingPortalSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ExpectedFailures(ResultStatus.Error, ResultStatus.NotFound)]
    public async Task<Result<BillingPortalSessionResponse>> CreatePortalSession(
        [FromBody] CreateBillingPortalSessionRequest request,
        CancellationToken cancellationToken = default
    ) =>
        await mediator.Send(
            new CreateBillingPortalSessionCommand(request.ReturnUrl),
            cancellationToken
        );

    [HttpGet("events")]
    [Produces("text/event-stream")]
    public async Task Stream(CancellationToken cancellationToken)
    {
        if (!tenantContext.IsResolved)
        {
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var tenantId = tenantContext.TenantId;
        var filtered = FilterByTenant(paymentEventStream.ReadAllAsync(cancellationToken), tenantId);

        try
        {
            await TypedResults
                .ServerSentEvents(filtered, eventType: "payment-status")
                .ExecuteAsync(HttpContext);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested
                || HttpContext.RequestAborted.IsCancellationRequested
            )
        {
            // Client disconnected.
        }
    }

    private static async IAsyncEnumerable<PaymentEventResponse> FilterByTenant(
        IAsyncEnumerable<PaymentEventResponse> source,
        Guid tenantId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        await foreach (var evt in source.WithCancellation(cancellationToken))
        {
            if (evt.TenantId == tenantId)
                yield return evt;
        }
    }
}
