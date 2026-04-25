namespace Homeless.Domain.Exceptions;

public sealed class TenantNotFoundException(string identifier)
    : DomainException("TENANT_NOT_FOUND", $"Tenant '{identifier}' was not found.");
