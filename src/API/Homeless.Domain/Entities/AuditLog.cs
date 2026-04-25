namespace Homeless.Domain.Entities;

/// <summary>
/// Immutable audit record captured by AuditBehavior for every write Command.
/// Stored in the audit_logs table for regulatory compliance and debugging.
/// The payload is sanitized (no passwords or secrets) before persistence.
/// </summary>
public sealed class AuditLog : Entity
{
    public string CommandName { get; private set; } = string.Empty;

    /// <summary>Serialized command payload (JSONB). Sensitive fields are stripped.</summary>
    public string Payload { get; private set; } = "{}";

    public Guid? UserId { get; private set; }
    public Guid? TenantId { get; private set; }
    public string? CorrelationId { get; private set; }
    public string Result { get; private set; } = string.Empty;

    private AuditLog() { }

    public static AuditLog Create(
        string commandName,
        string payload,
        Guid? userId,
        Guid? tenantId,
        string? correlationId,
        string result)
        => new()
        {
            Id = Guid.NewGuid(),
            CommandName = commandName,
            Payload = payload,
            UserId = userId,
            TenantId = tenantId,
            CorrelationId = correlationId,
            Result = result,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
}
