using Microsoft.Extensions.Options;
using Homeless.Application.Interfaces;

namespace Homeless.Infrastructure.Storage;

/// <summary>
/// Dev-only fallback: stores uploads in the local filesystem under the configured path.
/// </summary>
public sealed class LocalFileStorageService(IOptions<LocalStorageOptions> options) : IStorageService
{
    private readonly string _basePath = Path.GetFullPath(options.Value.LocalPath);

    public async Task<string> UploadAsync(
        Stream stream,
        string key,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(_basePath, key.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

        await using var fs = File.Create(filePath);
        await stream.CopyToAsync(fs, cancellationToken).ConfigureAwait(false);

        return $"/uploads/{key}";
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(_basePath, key.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(filePath)) File.Delete(filePath);
        return Task.CompletedTask;
    }

    public Task<string> GetPresignedUrlAsync(
        string key,
        TimeSpan expiry,
        CancellationToken cancellationToken = default) =>
        Task.FromResult($"/uploads/{key}");

    public Task<Stream> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(_basePath, key.Replace('/', Path.DirectorySeparatorChar));
        Stream stream = File.OpenRead(filePath);
        return Task.FromResult(stream);
    }

    public string GetPublicUrl(string key) => $"/uploads/{key}";
}

public sealed class LocalStorageOptions
{
    public const string SectionName = "Storage";
    public string LocalPath { get; set; } = "uploads";
}
