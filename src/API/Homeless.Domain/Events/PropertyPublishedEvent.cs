namespace Homeless.Domain.Events;

public sealed record PropertyPublishedEvent(Guid PropertyId, Guid TenantId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
