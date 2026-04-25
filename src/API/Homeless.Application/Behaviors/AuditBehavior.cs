using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using Homeless.Application.Interfaces;
using Homeless.Domain.Entities;

namespace Homeless.Application.Behaviors;

/// <summary>
/// Captures every write Command (anything implementing ICommand marker) before AND after execution.
/// Persists the audit entry via IAuditLogWriter which runs in an independent scope so audit records
/// survive even when the main transaction rolls back.
/// </summary>
public sealed class AuditBehavior<TRequest, TResponse>(
    ICurrentUserService currentUser,
    ITenantContext tenantContext,
    IAuditLogWriter auditWriter,
    ILogger<AuditBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    // Only audit write commands — requests whose name ends with "Command"
    private static bool ShouldAudit => typeof(TRequest).Name.EndsWith("Command", StringComparison.Ordinal);

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly HashSet<string> SensitiveFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "password", "secret", "token", "apiKey", "api_key", "creditCard", "cvv"
    };

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!ShouldAudit)
            return await next().ConfigureAwait(false);

        var commandName = typeof(TRequest).Name;
        var result = "Unknown";
        TResponse response;

        try
        {
            response = await next().ConfigureAwait(false);
            result = "Success";
        }
        catch (Exception ex)
        {
            result = $"Error: {ex.GetType().Name}";
            throw;
        }
        finally
        {
            try
            {
                var payload = SerializePayload(request);
                var correlationId = SerilogCorrelationId.Current;

                var entry = AuditLog.Create(
                    commandName: commandName,
                    payload: payload,
                    userId: currentUser.IsAuthenticated ? currentUser.UserId : null,
                    tenantId: tenantContext.IsResolved ? tenantContext.TenantId : null,
                    correlationId: correlationId,
                    result: result);

                await auditWriter.WriteAsync(entry, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception auditEx)
            {
                logger.LogWarning(auditEx, "Failed to write audit log for {CommandName}", commandName);
            }
        }

        return response;
    }

    private static string SerializePayload(TRequest request)
    {
        try
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
                JsonSerializer.Serialize(request, SerializerOptions), SerializerOptions);

            if (dict is null) return "{}";

            foreach (var key in SensitiveFields)
                if (dict.ContainsKey(key))
                    dict[key] = JsonDocument.Parse("\"[REDACTED]\"").RootElement;

            return JsonSerializer.Serialize(dict, SerializerOptions);
        }
        catch
        {
            return "{}";
        }
    }
}

/// <summary>
/// AsyncLocal correlation ID storage populated by CorrelationIdMiddleware.
/// AsyncLocal&lt;T&gt; flows across async continuations, unlike [ThreadStatic] which
/// is bound to a specific OS thread and loses context after an await resumes on a different thread.
/// </summary>
internal static class SerilogCorrelationId
{
    private static readonly AsyncLocal<string?> _current = new();

    public static string? Current
    {
        get => _current.Value;
        set => _current.Value = value;
    }
}
