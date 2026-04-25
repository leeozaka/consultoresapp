using System.Text.Json.Serialization;

namespace Homeless.Application.DTOs;

/// <summary>
/// Payload sent via Server-Sent Events when a property image has been
/// processed (WebP variants generated and stored).
/// </summary>
public sealed record ImageProcessedEvent(
    [property: JsonPropertyName("property_id")] Guid PropertyId,
    [property: JsonPropertyName("image_key")] string ImageKey,
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("thumbnail_url")] string ThumbnailUrl,
    [property: JsonPropertyName("medium_url")] string MediumUrl,
    [property: JsonPropertyName("is_processed")] bool IsProcessed
);
