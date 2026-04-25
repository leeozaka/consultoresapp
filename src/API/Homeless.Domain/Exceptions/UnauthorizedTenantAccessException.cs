namespace Homeless.Domain.Exceptions;

public sealed class UnauthorizedTenantAccessException()
    : DomainException("UNAUTHORIZED_TENANT_ACCESS", "Access to this resource is not allowed for the current tenant.");
