namespace Homeless.Domain.ValueObjects;

/// <summary>
/// Immutable value object representing an image attached to a Property.
/// Stored as part of a JSONB array in the properties table.
/// </summary>
public sealed record PropertyImage
{
    /// <summary>The R2 object key (path) of the original raw upload.</summary>
    public string Key { get; init; } = string.Empty;

    /// <summary>Public CDN URL of the processed full-size image (WebP).</summary>
    public string? Url { get; init; }

    /// <summary>Public CDN URL of the thumbnail (WebP, ~400px wide).</summary>
    public string? ThumbnailUrl { get; init; }

    /// <summary>Public CDN URL of the medium size (WebP, ~900px wide).</summary>
    public string? MediumUrl { get; init; }

    /// <summary>Display order within the property gallery.</summary>
    public int Order { get; init; }

    /// <summary>Original uploaded file name for reference.</summary>
    public string OriginalFileName { get; init; } = string.Empty;

    /// <summary>False while the background image processor is still converting to WebP.</summary>
    public bool IsProcessed { get; init; }

    public static PropertyImage CreatePending(string key, string originalFileName, int order) =>
        new() { Key = key, OriginalFileName = originalFileName, Order = order, IsProcessed = false };

    public PropertyImage MarkProcessed(string url, string thumbnailUrl, string mediumUrl) =>
        this with { Url = url, ThumbnailUrl = thumbnailUrl, MediumUrl = mediumUrl, IsProcessed = true };
}
