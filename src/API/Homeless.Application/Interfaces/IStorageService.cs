namespace Homeless.Application.Interfaces;

/// <summary>
/// Abstraction for object storage (Cloudflare R2 in production, local file system in dev).
/// </summary>
public interface IStorageService
{
    /// <summary>Uploads a stream and returns the public CDN URL.</summary>
    Task<string> UploadAsync(Stream content, string key, string contentType, CancellationToken cancellationToken = default);

    /// <summary>Deletes an object by its storage key.</summary>
    Task DeleteAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Returns a time-limited pre-signed URL for private access.</summary>
    Task<string> GetPresignedUrlAsync(string key, TimeSpan expiry, CancellationToken cancellationToken = default);

    /// <summary>Returns a stream for reading (used by image processor).</summary>
    Task<Stream> GetAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Returns the public (unauthenticated) URL for a given storage key.</summary>
    string GetPublicUrl(string key);
}
