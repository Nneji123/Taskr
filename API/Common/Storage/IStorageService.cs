namespace API.Common.Storage;

/// <summary>
/// Abstraction over file storage providers (S3, Cloudinary, local disk).
/// Provider is selected at startup via the <c>Storage:Provider</c> configuration value.
/// </summary>
public interface IStorageService
{
    /// <summary>Upload a file and return its public URL.</summary>
    Task<string> UploadAsync(Stream content, string fileName, string contentType, CancellationToken ct = default);

    /// <summary>Delete a file by its URL or key.</summary>
    Task DeleteAsync(string url, CancellationToken ct = default);

    /// <summary>Build a publicly accessible URL for a given key.</summary>
    string GetUrl(string key);
}
