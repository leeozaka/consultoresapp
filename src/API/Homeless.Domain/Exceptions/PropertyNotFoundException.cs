namespace Homeless.Domain.Exceptions;

public sealed class PropertyNotFoundException(Guid id)
    : DomainException("PROPERTY_NOT_FOUND", $"Property '{id}' was not found.");
