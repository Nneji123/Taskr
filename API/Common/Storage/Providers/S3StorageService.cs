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
        _client = new AmazonS3Client(
            _s3.AccessKey, _s3.SecretKey,
            new AmazonS3Config { RegionEndpoint = RegionEndpoint.GetBySystemName(_s3.Region) });
    }

    public async Task<string> UploadAsync(Stream content, string fileName, string contentType, CancellationToken ct = default)
    {
        var key = $"{Guid.NewGuid():N}_{fileName}";

        await _client.PutObjectAsync(new PutObjectRequest
        {
            InputStream = content,
            BucketName = _s3.BucketName,
            Key = key,
            ContentType = contentType,
            CannedACL = S3CannedACL.PublicRead
        }, ct);

        var url = string.IsNullOrWhiteSpace(_s3.BaseUrl)
            ? $"https://{_s3.BucketName}.s3.{_s3.Region}.amazonaws.com/{key}"
            : $"{_s3.BaseUrl.TrimEnd('/')}/{key}";

        _logger.LogInformation("Uploaded {Key} to S3 bucket {Bucket}", key, _s3.BucketName);
        return url;
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

    private static string? ExtractKey(string url)
    {
        var uri = new Uri(url);
        var segments = uri.AbsolutePath.TrimStart('/').Split('/');
        return segments.Length > 0 ? segments[^1] : null;
    }
}
