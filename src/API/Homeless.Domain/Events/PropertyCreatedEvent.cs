namespace Homeless.Domain.Events;

public sealed record PropertyCreatedEvent(Guid PropertyId, Guid TenantId, string Title) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
