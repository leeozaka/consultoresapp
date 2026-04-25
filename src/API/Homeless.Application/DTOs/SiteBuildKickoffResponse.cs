using System.Text.Json.Serialization;

namespace Homeless.Application.DTOs;

public sealed record SiteBuildKickoffResponse(
    [property: JsonPropertyName("tenant_id")] Guid TenantId,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("message")] string Message
);
