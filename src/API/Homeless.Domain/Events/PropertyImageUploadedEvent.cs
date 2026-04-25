namespace Homeless.Domain.Events;

public sealed record PropertyImageUploadedEvent(
    Guid PropertyId,
    Guid TenantId,
    string ImageKey,
    string OriginalFileName
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
