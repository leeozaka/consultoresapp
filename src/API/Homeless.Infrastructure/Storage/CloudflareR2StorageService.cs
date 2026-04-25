using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using Homeless.Application.Interfaces;
using Homeless.Infrastructure.Storage.Options;

namespace Homeless.Infrastructure.Storage;

public sealed class CloudflareR2StorageService(
    IAmazonS3 s3Client,
    IOptions<R2Options> options) : IStorageService
{
    private readonly R2Options _opts = options.Value;

    public async Task<string> UploadAsync(
        Stream stream,
        string key,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var request = new PutObjectRequest
        {
            BucketName = _opts.BucketName,
            Key = key,
            InputStream = stream,
            ContentType = contentType,
            DisablePayloadSigning = !IsLocalStack // required by R2, not by LocalStack
        };

        await s3Client.PutObjectAsync(request, cancellationToken).ConfigureAwait(false);
        return $"{_opts.PublicBaseUrl.TrimEnd('/')}/{key}";
    }

    private bool IsLocalStack =>
        _opts.ResolvedServiceUrl.Contains("localhost", StringComparison.OrdinalIgnoreCase) ||
        _opts.ResolvedServiceUrl.Contains("localstack", StringComparison.OrdinalIgnoreCase);

    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        var request = new DeleteObjectRequest
        {
            BucketName = _opts.BucketName,
            Key = key
        };

        await s3Client.DeleteObjectAsync(request, cancellationToken).ConfigureAwait(false);
    }

    public async Task<string> GetPresignedUrlAsync(
        string key,
        TimeSpan expiry,
        CancellationToken cancellationToken = default)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _opts.BucketName,
            Key = key,
            Expires = DateTime.UtcNow.Add(expiry),
            // LocalStack runs on plain HTTP; Cloudflare R2 requires HTTPS.
            Protocol = IsLocalStack ? Protocol.HTTP : Protocol.HTTPS
        };

        return await Task.FromResult(s3Client.GetPreSignedURL(request)).ConfigureAwait(false);
    }

    public string GetPublicUrl(string key) => $"{_opts.PublicBaseUrl.TrimEnd('/')}/{key}";

    public async Task<Stream> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        var request = new GetObjectRequest
        {
            BucketName = _opts.BucketName,
            Key = key
        };

        var response = await s3Client.GetObjectAsync(request, cancellationToken).ConfigureAwait(false);
        return response.ResponseStream;
    }
}
