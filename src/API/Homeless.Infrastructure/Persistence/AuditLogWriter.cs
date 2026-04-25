using Microsoft.Extensions.DependencyInjection;
using Homeless.Application.Interfaces;
using Homeless.Domain.Entities;
using Homeless.Infrastructure.Persistence.Context;

namespace Homeless.Infrastructure.Persistence;

/// <summary>
/// Persists audit log entries in an isolated DI scope so they survive
/// even when the main command transaction rolls back.
/// </summary>
public sealed class AuditLogWriter(IServiceScopeFactory scopeFactory) : IAuditLogWriter
{
    public async Task WriteAsync(AuditLog entry, CancellationToken cancellationToken = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.AuditLogs.Add(entry);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
