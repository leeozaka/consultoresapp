using MassTransit;

namespace Homeless.Application.Sagas.Payment;

public sealed class PaymentStateMachine : MassTransitStateMachine<PaymentSagaState>
{
    public State CheckoutCreated { get; private set; } = null!;
    public State WaitingForPayment { get; private set; } = null!;
    public State ActivatingSubscription { get; private set; } = null!;
    public State Completed { get; private set; } = null!;
    public State Faulted { get; private set; } = null!;

    public Event<CheckoutSessionRequested> CheckoutSessionRequested { get; private set; } = null!;
    public Event<CheckoutSessionCreated> CheckoutSessionCreated { get; private set; } = null!;
    public Event<CheckoutSessionFailed> CheckoutSessionFailed { get; private set; } = null!;
    public Event<PaymentWebhookReceived> PaymentWebhookReceived { get; private set; } = null!;
    public Event<SubscriptionActivated> SubscriptionActivated { get; private set; } = null!;
    public Event<SubscriptionActivationFailed> SubscriptionActivationFailed { get; private set; } =
        null!;

    public Schedule<PaymentSagaState, PaymentSessionTimedOut> CheckoutTimeout
    {
        get;
        private set;
    } = null!;

    public PaymentStateMachine()
    {
        InstanceState(x => x.CurrentState);

        Event(
            () => CheckoutSessionRequested,
            x => x.CorrelateById(ctx => ctx.Message.CorrelationId)
        );
        Event(() => CheckoutSessionCreated, x => x.CorrelateById(ctx => ctx.Message.CorrelationId));
        Event(() => CheckoutSessionFailed, x => x.CorrelateById(ctx => ctx.Message.CorrelationId));
        Event(() => PaymentWebhookReceived, x => x.CorrelateById(ctx => ctx.Message.CorrelationId));
        Event(() => SubscriptionActivated, x => x.CorrelateById(ctx => ctx.Message.CorrelationId));
        Event(
            () => SubscriptionActivationFailed,
            x => x.CorrelateById(ctx => ctx.Message.CorrelationId)
        );

        // 25h > Stripe's 24h session lifetime — fires if no webhook is ever delivered
        Schedule(
            () => CheckoutTimeout,
            x => x.CheckoutExpirationTokenId,
            s =>
            {
                s.Delay = TimeSpan.FromHours(25);
                s.Received = e => e.CorrelateById(ctx => ctx.Message.CorrelationId);
            }
        );

        Initially(
            When(CheckoutSessionRequested)
                .Then(ctx =>
                {
                    ctx.Saga.TenantId = ctx.Message.TenantId;
                    ctx.Saga.PlanId = ctx.Message.PlanId;
                    ctx.Saga.Amount = ctx.Message.Amount;
                    ctx.Saga.Currency = ctx.Message.Currency;
                    ctx.Saga.CreatedAt = DateTime.UtcNow;
                    ctx.Saga.UpdatedAt = DateTime.UtcNow;
                })
                .PublishAsync(ctx =>
                    ctx.Init<CreateCheckoutSessionCommand>(
                        new
                        {
                            ctx.Saga.CorrelationId,
                            ctx.Saga.TenantId,
                            ctx.Saga.PlanId,
                            ctx.Saga.Amount,
                            ctx.Saga.Currency,
                            ctx.Message.SuccessUrl,
                            ctx.Message.CancelUrl,
                        }
                    )
                )
                .TransitionTo(CheckoutCreated)
        );

        During(
            CheckoutCreated,
            When(CheckoutSessionCreated)
                .Then(ctx =>
                {
                    ctx.Saga.StripeSessionId = ctx.Message.StripeSessionId;
                    ctx.Saga.SessionUrl = ctx.Message.SessionUrl;
                    ctx.Saga.PaymentIntentId = ctx.Message.PaymentIntentId;
                    ctx.Saga.UpdatedAt = DateTime.UtcNow;
                })
                .Schedule(
                    CheckoutTimeout,
                    ctx => ctx.Init<PaymentSessionTimedOut>(new { ctx.Saga.CorrelationId })
                )
                .TransitionTo(WaitingForPayment),
            When(CheckoutSessionFailed)
                .Then(ctx =>
                {
                    ctx.Saga.FailureReason = ctx.Message.Reason;
                    ctx.Saga.UpdatedAt = DateTime.UtcNow;
                })
                .PublishAsync(ctx =>
                    ctx.Init<PaymentSagaFailed>(
                        new
                        {
                            ctx.Saga.CorrelationId,
                            ctx.Saga.TenantId,
                            Reason = ctx.Saga.FailureReason!,
                        }
                    )
                )
                .TransitionTo(Faulted)
                .Finalize()
        );

        During(
            WaitingForPayment,
            When(
                    PaymentWebhookReceived,
                    ctx =>
                        ctx.Message.EventType == "checkout.session.completed"
                        && ctx.Message.Status == "complete"
                )
                .Unschedule(CheckoutTimeout)
                .Then(ctx => ctx.Saga.UpdatedAt = DateTime.UtcNow)
                .PublishAsync(ctx =>
                    ctx.Init<ActivateSubscriptionCommand>(
                        new
                        {
                            ctx.Saga.CorrelationId,
                            ctx.Saga.TenantId,
                            ctx.Saga.PlanId,
                        }
                    )
                )
                .TransitionTo(ActivatingSubscription),
            When(PaymentWebhookReceived, ctx => ctx.Message.EventType == "checkout.session.expired")
                .Unschedule(CheckoutTimeout)
                .Then(ctx =>
                {
                    ctx.Saga.FailureReason = "Payment session expired";
                    ctx.Saga.UpdatedAt = DateTime.UtcNow;
                })
                .PublishAsync(ctx =>
                    ctx.Init<PaymentSagaFailed>(
                        new
                        {
                            ctx.Saga.CorrelationId,
                            ctx.Saga.TenantId,
                            Reason = "Payment session expired",
                        }
                    )
                )
                .TransitionTo(Faulted)
                .Finalize(),
            When(CheckoutTimeout.Received)
                .Then(ctx =>
                {
                    ctx.Saga.FailureReason =
                        "Checkout session timed out — no webhook received after 25 hours.";
                    ctx.Saga.UpdatedAt = DateTime.UtcNow;
                })
                .PublishAsync(ctx =>
                    ctx.Init<PaymentSagaFailed>(
                        new
                        {
                            ctx.Saga.CorrelationId,
                            ctx.Saga.TenantId,
                            Reason = ctx.Saga.FailureReason!,
                        }
                    )
                )
                .TransitionTo(Faulted)
                .Finalize()
        );

        During(
            ActivatingSubscription,
            When(SubscriptionActivated)
                .Then(ctx => ctx.Saga.UpdatedAt = DateTime.UtcNow)
                .PublishAsync(ctx =>
                    ctx.Init<PaymentSagaCompleted>(
                        new
                        {
                            ctx.Saga.CorrelationId,
                            ctx.Saga.TenantId,
                            ctx.Saga.PlanId,
                            ctx.Saga.StripeSessionId,
                        }
                    )
                )
                .TransitionTo(Completed)
                .Finalize(),
            When(SubscriptionActivationFailed)
                .Then(ctx =>
                {
                    ctx.Saga.FailureReason = ctx.Message.Reason;
                    ctx.Saga.UpdatedAt = DateTime.UtcNow;
                })
                .PublishAsync(ctx =>
                    ctx.Init<PaymentSagaFailed>(
                        new
                        {
                            ctx.Saga.CorrelationId,
                            ctx.Saga.TenantId,
                            Reason = ctx.Saga.FailureReason!,
                        }
                    )
                )
                .TransitionTo(Faulted)
                .Finalize()
        );

        SetCompletedWhenFinalized();
    }
}
