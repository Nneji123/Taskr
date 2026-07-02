using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using API.Options;

namespace API.Common.Storage.Providers;

public class S3StorageService : IStorageService
{
    private readonly S3StorageSettings _s3;
    private readonly AmazonS3Client _client;
    private readonly ILogger<S3StorageService> _logger;

    public S3StorageService(IOptions<StorageOptions> options, ILogger<S3StorageService> logger)
    {
        _s3 = options.Value.S3;
        _logger = logger;

        var config = new AmazonS3Config();
        if (!string.IsNullOrWhiteSpace(_s3.ServiceUrl))
        {
            config.ServiceURL = _s3.ServiceUrl;
        }
        else
        {
            config.RegionEndpoint = RegionEndpoint.GetBySystemName(_s3.Region);
        }

        _client = new AmazonS3Client(_s3.AccessKey, _s3.SecretKey, config);
    }

    private static readonly TimeSpan DefaultUploadSignedUrlTtl = TimeSpan.FromMinutes(5);

    public async Task<string> UploadAsync(Stream content, string fileName, string contentType, CancellationToken ct = default)
    {
        var key = $"{Guid.NewGuid():N}_{fileName}";

        await _client.PutObjectAsync(new PutObjectRequest
        {
            InputStream = content,
            BucketName = _s3.BucketName,
            Key = key,
            ContentType = contentType
        }, ct);

        _logger.LogInformation("Uploaded {Key} to S3 bucket {Bucket}", key, _s3.BucketName);
        return GetSignedUrl(key, DefaultUploadSignedUrlTtl);
    }

    public async Task DeleteAsync(string url, CancellationToken ct = default)
    {
        var key = ExtractKey(url);
        if (string.IsNullOrEmpty(key)) return;

        await _client.DeleteObjectAsync(new DeleteObjectRequest
        {
            BucketName = _s3.BucketName,
            Key = key
        }, ct);

        _logger.LogInformation("Deleted {Key} from S3 bucket {Bucket}", key, _s3.BucketName);
    }

    public string GetUrl(string key) => string.IsNullOrWhiteSpace(_s3.BaseUrl)
        ? $"https://{_s3.BucketName}.s3.{_s3.Region}.amazonaws.com/{key}"
        : $"{_s3.BaseUrl.TrimEnd('/')}/{key}";

    private static readonly TimeSpan MaxSignedUrlTtl = TimeSpan.FromHours(1);
    private static readonly TimeSpan MinSignedUrlTtl = TimeSpan.FromSeconds(30);

    public string GetSignedUrl(string key, TimeSpan expiresIn)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Storage key is required.", nameof(key));

        var ttl = expiresIn;
        if (ttl < MinSignedUrlTtl) ttl = MinSignedUrlTtl;
        if (ttl > MaxSignedUrlTtl) ttl = MaxSignedUrlTtl;

        var request = new GetPreSignedUrlRequest
        {
            BucketName = _s3.BucketName,
            Key = key,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.Add(ttl),
            Protocol = Protocol.HTTPS
        };

        var url = _client.GetPreSignedURL(request);

        if (!string.IsNullOrWhiteSpace(_s3.BaseUrl))
        {
            var prefix = _s3.BaseUrl.TrimEnd('/');
            var keyIndex = url.IndexOf($"/{key}", StringComparison.Ordinal);
            if (keyIndex >= 0)
                url = prefix + url[keyIndex..];
        }

        _logger.LogInformation("Generated signed URL for {Key} (TTL: {Ttl}s)", key, (int)ttl.TotalSeconds);
        return url;
    }

    private static string? ExtractKey(string url)
    {
        var uri = new Uri(url);
        var segments = uri.AbsolutePath.TrimStart('/').Split('/');
        return segments.Length > 0 ? segments[^1] : null;
    }
}
