using Homeless.Domain.Entities;

namespace Homeless.Application.Interfaces;

/// <summary>
/// Dedicated writer for audit logs, isolated from the main command DbContext scope.
/// Uses its own IServiceScope so audit entries persist even when the main transaction rolls back.
/// </summary>
public interface IAuditLogWriter
{
    Task WriteAsync(AuditLog entry, CancellationToken cancellationToken = default);
}
