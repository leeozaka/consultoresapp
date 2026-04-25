using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Homeless.Infrastructure.Auth;

/// <summary>
/// Manages OpenIddict certificates in Redis for multi-replica deployments.
/// Ensures all pods use the same signing/encryption certificates, preventing
/// "invalid_grant" errors when tokens are validated across different instances.
/// </summary>
public sealed class OpenIddictCertificateStore
{
    private const string SigningCertificateCacheKey = "openiddict:certificates:signing";
    private const string EncryptionCertificateCacheKey = "openiddict:certificates:encryption";
    private const long CertificateCacheDuration = 365 * 24 * 60 * 60; // 1 year in seconds

    private readonly IConnectionMultiplexer? _connectionMultiplexer;
    private readonly ILoggerFactory? _loggerFactory;
    private ILogger<OpenIddictCertificateStore>? _logger;

    public OpenIddictCertificateStore(
        IConnectionMultiplexer? connectionMultiplexer,
        ILoggerFactory? loggerFactory = null)
    {
        _connectionMultiplexer = connectionMultiplexer;
        _loggerFactory = loggerFactory;
    }

    private ILogger<OpenIddictCertificateStore> Logger =>
        _logger ??= _loggerFactory?.CreateLogger<OpenIddictCertificateStore>() ??
                    new NoOpLogger();

    /// <summary>
    /// Synchronously gets or creates a signing certificate from Redis cache.
    /// </summary>
    public X509Certificate2 GetOrCreateSigningCertificate()
    {
        Logger.LogInformation("Checking Redis for cached signing certificate");
        return GetOrCreateCertificate(SigningCertificateCacheKey, "OpenIddict Signing Certificate");
    }

    /// <summary>
    /// Synchronously gets or creates an encryption certificate from Redis cache.
    /// </summary>
    public X509Certificate2 GetOrCreateEncryptionCertificate()
    {
        Logger.LogInformation("Checking Redis for cached encryption certificate");
        return GetOrCreateCertificate(EncryptionCertificateCacheKey, "OpenIddict Encryption Certificate");
    }

    /// <summary>
    /// Synchronously gets or creates a certificate from Redis cache.
    /// </summary>
    private X509Certificate2 GetOrCreateCertificate(string cacheKey, string subjectName)
    {
        try
        {
            // If Redis is not available, fall back to in-memory certificate
            if (_connectionMultiplexer == null)
            {
                Logger.LogWarning("Redis not available, using in-memory development certificate");
                return CreateDevelopmentCertificate(subjectName);
            }

            var database = _connectionMultiplexer.GetDatabase();

            // Try to get cached certificate from Redis
            var cachedCertB64 = database.StringGet(cacheKey);
            if (cachedCertB64.HasValue)
            {
                try
                {
                    var cert = new X509Certificate2(Convert.FromBase64String(cachedCertB64.ToString()));
                    Logger.LogInformation("Loaded certificate from Redis cache: {Subject}", cert.Subject);
                    return cert;
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "Failed to load certificate from Redis cache, regenerating");
                    database.KeyDelete(cacheKey);
                }
            }

            // Certificate not in cache or failed to load — generate a new one
            Logger.LogInformation("Generating new development certificate for {SubjectName}", subjectName);
            var newCert = CreateDevelopmentCertificate(subjectName);

            // Store in Redis for other pods to use
            var certB64 = Convert.ToBase64String(newCert.Export(X509ContentType.Pfx));
            var cacheDuration = TimeSpan.FromSeconds(CertificateCacheDuration);
            database.StringSet(cacheKey, certB64, cacheDuration);

            Logger.LogInformation(
                "Generated and cached certificate: {Subject} (expires {Expiration})",
                newCert.Subject,
                newCert.NotAfter
            );
            return newCert;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error in OpenIddict certificate store, falling back to in-memory certificate");
            // Fallback to in-memory certificate if Redis fails
            return CreateDevelopmentCertificate(subjectName);
        }
    }

    /// <summary>
    /// Creates a self-signed development certificate.
    /// </summary>
    private static X509Certificate2 CreateDevelopmentCertificate(string subjectName)
    {
        using var algorithm = RSA.Create(4096);

        var request = new CertificateRequest(
            $"CN={subjectName}",
            algorithm,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        // Create self-signed certificate valid for 1 year
        var cert = request.CreateSelfSigned(
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddYears(1)
        );

        return cert;
    }
}

/// <summary>
/// No-op logger used as fallback when ILoggerFactory is not available during DI setup.
/// </summary>
internal sealed class NoOpLogger : ILogger<OpenIddictCertificateStore>
{
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        // No-op
    }

    public bool IsEnabled(LogLevel logLevel) => false;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }
}
