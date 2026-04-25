using System.Collections.Concurrent;
using Homeless.Application.Interfaces;
using Homeless.Application.Options;
using Homeless.Application.Sagas.Payment;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Homeless.Infrastructure.Sagas;

public sealed class PaymentSagaService : IPaymentSagaService, IAsyncDisposable
{
    private readonly IBus _bus;
    private readonly ICacheService _cacheService;
    private readonly ILogger<PaymentSagaService> _logger;
    private readonly TimeSpan _sagaTimeout;
    private readonly ConcurrentDictionary<Guid, TaskCompletionSource<PaymentSagaResult>> _pending =
        new();
    private IReceiveEndpoint? _responseEndpoint;
    private readonly SemaphoreSlim _endpointLock = new(1, 1);

    public PaymentSagaService(
        IBus bus,
        ICacheService cacheService,
        IOptions<ConcurrencyOptions> concurrencyOptions,
        ILogger<PaymentSagaService> logger
    )
    {
        _bus = bus;
        _cacheService = cacheService;
        _logger = logger;
        _sagaTimeout = TimeSpan.FromSeconds(concurrencyOptions.Value.SagaTimeoutSeconds);
    }

    public async Task<PaymentSagaResult> InitiateCheckoutAsync(
        Guid tenantId,
        Guid planId,
        long amount,
        string currency,
        string successUrl,
        string cancelUrl,
        CancellationToken cancellationToken = default
    ) =>
        await InitiateCheckoutCoreAsync(
                tenantId,
                referenceKey: $"plan:{planId}",
                new CheckoutSessionRequested
                {
                    CorrelationId = Guid.NewGuid(),
                    TenantId = tenantId,
                    PlanId = planId,
                    Amount = amount,
                    Currency = currency,
                    SuccessUrl = successUrl,
                    CancelUrl = cancelUrl,
                },
                cancellationToken
            )
            .ConfigureAwait(false);

    private async Task<PaymentSagaResult> InitiateCheckoutCoreAsync(
        Guid tenantId,
        string referenceKey,
        CheckoutSessionRequested request,
        CancellationToken cancellationToken
    )
    {
        await EnsureEndpointAsync(cancellationToken).ConfigureAwait(false);

        var correlationId = request.CorrelationId;
        var idempotencyKey = CacheKeys.Idempotency($"checkout:{tenantId}:{referenceKey}");
        var cached = await _cacheService
            .GetAsync<PaymentSagaResult>(idempotencyKey, cancellationToken)
            .ConfigureAwait(false);

        if (cached is not null)
        {
            _logger.LogInformation(
                "Returning cached checkout result for tenant {TenantId}",
                tenantId
            );
            return cached;
        }

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_sagaTimeout);

        var tcs = new TaskCompletionSource<PaymentSagaResult>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        _pending.TryAdd(correlationId, tcs);

        try
        {
            await _bus.Publish(request, cancellationToken).ConfigureAwait(false);

            await using (cts.Token.Register(() => tcs.TrySetCanceled(cts.Token)))
            {
                var result = await tcs.Task.ConfigureAwait(false);
                await _cacheService
                    .SetAsync(idempotencyKey, result, TimeSpan.FromMinutes(10), cancellationToken)
                    .ConfigureAwait(false);
                return result;
            }
        }
        finally
        {
            _pending.TryRemove(correlationId, out _);
        }
    }

    private async Task EnsureEndpointAsync(CancellationToken cancellationToken)
    {
        if (_responseEndpoint is not null)
            return;

        await _endpointLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_responseEndpoint is not null)
                return;

            var handle = _bus.ConnectReceiveEndpoint(
                "payment-responses-shared",
                cfg =>
                {
                    cfg.Handler<CheckoutSessionCreated>(ctx =>
                    {
                        if (_pending.TryGetValue(ctx.Message.CorrelationId, out var tcs))
                        {
                            tcs.TrySetResult(
                                new PaymentSagaResult(
                                    Success: true,
                                    SessionUrl: ctx.Message.SessionUrl,
                                    StripeSessionId: ctx.Message.StripeSessionId,
                                    ErrorMessage: null
                                )
                            );
                        }
                        return Task.CompletedTask;
                    });

                    cfg.Handler<CheckoutSessionFailed>(ctx =>
                    {
                        if (_pending.TryGetValue(ctx.Message.CorrelationId, out var tcs))
                        {
                            tcs.TrySetResult(
                                new PaymentSagaResult(
                                    Success: false,
                                    SessionUrl: null,
                                    StripeSessionId: null,
                                    ErrorMessage: ctx.Message.Reason
                                )
                            );
                        }
                        return Task.CompletedTask;
                    });
                }
            );

            await handle.Ready.ConfigureAwait(false);
            _responseEndpoint = handle.ReceiveEndpoint;
        }
        finally
        {
            _endpointLock.Release();
        }
    }

    public ValueTask DisposeAsync()
    {
        _endpointLock.Dispose();
        return ValueTask.CompletedTask;
    }
}
