using Homeless.Domain.Events;

namespace Homeless.Application.Interfaces;

public interface IEventBus
{
    ValueTask PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);
    ValueTask PublishAllAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}
