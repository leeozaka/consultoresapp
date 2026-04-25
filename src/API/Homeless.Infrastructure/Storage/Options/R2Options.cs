namespace Homeless.Infrastructure.Storage.Options;

public sealed class R2Options
{
    public const string SectionName = "R2";

    public string AccountId { get; set; } = string.Empty;
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string BucketName { get; set; } = string.Empty;
    public string PublicBaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// S3-compatible service URL. When set explicitly (e.g. for LocalStack),
    /// the configured value is used; otherwise it falls back to the
    /// Cloudflare R2 endpoint derived from <see cref="AccountId"/>.
    /// </summary>
    public string ServiceUrl { get; set; } = string.Empty;

    internal string ResolvedServiceUrl =>
        !string.IsNullOrWhiteSpace(ServiceUrl)
            ? ServiceUrl
            : $"https://{AccountId}.r2.cloudflarestorage.com";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(AccountId) &&
        !string.IsNullOrWhiteSpace(AccessKey);
}
