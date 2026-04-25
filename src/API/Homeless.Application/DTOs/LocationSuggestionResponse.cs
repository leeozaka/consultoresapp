using System.Text.Json.Serialization;

namespace Homeless.Application.DTOs;

public sealed record LocationSuggestionResponse(
    [property: JsonPropertyName("label")] string Label,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("city")] string City,
    [property: JsonPropertyName("state")] string State
);
