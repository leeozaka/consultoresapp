namespace Homeless.Domain.Events;

public sealed record TenantSuspendedEvent(Guid TenantId, string Reason) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
