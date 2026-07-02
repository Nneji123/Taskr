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

    /// <summary>
    /// Build a time-limited, pre-signed URL for a given key. Callers should fetch a
    /// signed URL only when they are about to display or download the object, not for
    /// long-lived storage. Provider implementations cap the requested TTL.
    /// </summary>
    /// <param name="key">Storage key of the object to sign (e.g. <c>abc123_photo.jpg</c>).</param>
    /// <param name="expiresIn">Requested validity window. Providers clamp this to a safe maximum.</param>
    /// <returns>URL that grants read access until the expiry.</returns>
    string GetSignedUrl(string key, TimeSpan expiresIn);
}
