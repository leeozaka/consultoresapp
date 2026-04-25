using MassTransit;
using Microsoft.Extensions.Logging;
using Homeless.Application.Interfaces;
using Homeless.Domain.Events;

namespace Homeless.Infrastructure.Messaging;

public sealed class MassTransitEventBus(
    IPublishEndpoint publishEndpoint,
    ILogger<MassTransitEventBus> logger) : IEventBus
{
    public async ValueTask PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        await publishEndpoint.Publish(domainEvent, domainEvent.GetType(), cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Event published: {EventType} (ID: {EventId}) at {OccurredAt}",
            domainEvent.GetType().Name,
            domainEvent.EventId,
            domainEvent.OccurredAt);
    }

    public async ValueTask PublishAllAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in domainEvents)
            await PublishAsync(domainEvent, cancellationToken).ConfigureAwait(false);
    }
}
