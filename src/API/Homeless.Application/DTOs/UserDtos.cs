using System.Text.Json.Serialization;

namespace Homeless.Application.DTOs;

public sealed record UserResponse(
    [property: JsonPropertyName("id")]           Guid                    Id,
    [property: JsonPropertyName("email")]         string                  Email,
    [property: JsonPropertyName("first_name")]     string                  FirstName,
    [property: JsonPropertyName("last_name")]     string                  LastName,
    [property: JsonPropertyName("tenant_id")]     Guid?                   TenantId,
    [property: JsonPropertyName("tenant_name")]   string?                 TenantName,
    [property: JsonPropertyName("roles")]         IReadOnlyList<string>   Roles,
    [property: JsonPropertyName("is_active")]     bool                    IsActive,
    [property: JsonPropertyName("created_at")]    DateTime                CreatedAt
);

public sealed record AssignTenantRequest(
    [property: JsonPropertyName("tenant_id")] Guid   TenantId,
    [property: JsonPropertyName("role")]      string Role
);

public sealed record RegisterUserRequest(
    [property: JsonPropertyName("email")]      string  Email,
    [property: JsonPropertyName("password")]   string  Password,
    [property: JsonPropertyName("first_name")]  string? FirstName = null,
    [property: JsonPropertyName("last_name")]  string? LastName  = null
);

public sealed record CreateUserRequest(
    [property: JsonPropertyName("email")]      string  Email,
    [property: JsonPropertyName("password")]   string  Password,
    [property: JsonPropertyName("first_name")]  string? FirstName = null,
    [property: JsonPropertyName("last_name")]  string? LastName  = null,
    [property: JsonPropertyName("tenant_id")]  Guid?   TenantId  = null,
    [property: JsonPropertyName("role")]       string? Role      = null
);

public sealed record UpdateUserRequest(
    [property: JsonPropertyName("first_name")]  string? FirstName = null,
    [property: JsonPropertyName("last_name")]  string? LastName  = null,
    [property: JsonPropertyName("email")]      string? Email     = null
);
