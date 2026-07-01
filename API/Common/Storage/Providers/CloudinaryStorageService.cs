using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Options;
using API.Options;

namespace API.Common.Storage.Providers;

public class CloudinaryStorageService : IStorageService
{
    private readonly CloudinarySettings _c;
    private readonly Cloudinary _cloudinary;
    private readonly ILogger<CloudinaryStorageService> _logger;

    public CloudinaryStorageService(IOptions<StorageOptions> options, ILogger<CloudinaryStorageService> logger)
    {
        _c = options.Value.Cloudinary;
        _logger = logger;
        _cloudinary = new Cloudinary(new Account(_c.CloudName, _c.ApiKey, _c.ApiSecret));
    }

    public async Task<string> UploadAsync(Stream content, string fileName, string contentType, CancellationToken ct = default)
    {
        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(fileName, content),
            Folder = _c.Folder,
            PublicId = Guid.NewGuid().ToString("N"),
            Overwrite = false
        };

        var result = await _cloudinary.UploadAsync(uploadParams, ct);

        if (result.Error is not null)
            throw new InvalidOperationException($"Cloudinary upload failed: {result.Error.Message}");

        _logger.LogInformation("Uploaded {PublicId} to Cloudinary folder {Folder}", result.PublicId, _c.Folder);
        return result.SecureUrl.AbsoluteUri;
    }

    public async Task DeleteAsync(string url, CancellationToken ct = default)
    {
        var publicId = ExtractPublicId(url);
        if (string.IsNullOrEmpty(publicId)) return;

        var result = await _cloudinary.DestroyAsync(new DeletionParams(publicId));

        if (result.Error is not null)
            _logger.LogWarning("Cloudinary delete failed for {PublicId}: {Error}", publicId, result.Error.Message);
        else
            _logger.LogInformation("Deleted {PublicId} from Cloudinary", publicId);
    }

    public string GetUrl(string key) => $"https://res.cloudinary.com/{_c.CloudName}/image/upload/{_c.Folder}/{key}";

    private static string? ExtractPublicId(string url)
    {
        var uri = new Uri(url);
        var segments = uri.AbsolutePath.TrimStart('/').Split('/');
        var last = segments.LastOrDefault()?.Split('.')[0];
        return last;
    }
}
