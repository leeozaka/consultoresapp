using System.Text.Json.Serialization;

namespace Homeless.Application.DTOs;

/// <summary>
/// Keyset-paginated response. Pass <see cref="NextCursor"/> as the <c>after</c> query parameter
/// on the next request to retrieve the following page.
/// </summary>
public sealed record CursorPaginatedResponse<T>(
    [property: JsonPropertyName("items")] IReadOnlyList<T> Items,
    [property: JsonPropertyName("next_cursor")] string? NextCursor,
    [property: JsonPropertyName("has_next_page")] bool HasNextPage
);
