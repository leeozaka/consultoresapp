namespace Homeless.Domain.Events;

public sealed record TenantActivatedEvent(Guid TenantId, string Slug) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
