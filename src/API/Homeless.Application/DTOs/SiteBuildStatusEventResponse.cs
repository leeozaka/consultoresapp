using System.Text.Json.Serialization;

namespace Homeless.Application.DTOs;

public sealed record SiteBuildStatusEventResponse(
    [property: JsonPropertyName("tenant_id")] Guid TenantId,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("step")] string Step,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("occurred_at")] DateTime OccurredAt
);
