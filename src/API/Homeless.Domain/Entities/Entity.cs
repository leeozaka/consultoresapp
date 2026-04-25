using System.Text.Json.Serialization;
using Homeless.Domain.Events;

namespace Homeless.Domain.Entities;

public abstract class Entity
{
    [JsonInclude]
    public Guid Id { get; protected set; }
    [JsonInclude]
    public DateTime CreatedAt { get; protected set; }
    [JsonInclude]
    public DateTime UpdatedAt { get; protected set; }

    [JsonInclude]
    public bool IsDeleted { get; private set; }

    [JsonInclude]
    public DateTime? DeletedAt { get; private set; }

    public void SoftDelete()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
    }

    public void Restore()
    {
        IsDeleted = false;
        DeletedAt = null;
    }

    private readonly List<IDomainEvent> _domainEvents = [];
    [JsonIgnore]
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
