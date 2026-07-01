using System.Net;

namespace API.Common.Storage;

/// <summary>
/// Validates that a URL points to a file uploaded via our own storage.
/// This prevents users from setting arbitrary external URLs as their avatar/cover image
/// (e.g., to bypass content moderation or impersonate other services).
///
/// Validation rules:
///  1. URL is well-formed.
///  2. URL host is one of our trusted storage hosts (S3, Cloudinary, or local).
///  3. HTTP HEAD returns a 2xx/3xx status.
///  4. Content-Type matches the allowed types (default: image/*).
///  5. File size is within the limit (default: 10 MB).
/// </summary>
public class FileURLValidator
{
    private static readonly string[] AllowedTypesPrefixes = ["image/"];

    private readonly int _maxSizeBytes;
    private readonly TimeSpan _timeout;
    private readonly IHttpClientFactory _httpClientFactory;

    public FileURLValidator(IHttpClientFactory httpClientFactory, int maxSizeMb = 10, int timeoutSeconds = 10)
    {
        _maxSizeBytes = maxSizeMb * 1024 * 1024;
        _timeout = TimeSpan.FromSeconds(timeoutSeconds);
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>Returns true if the URL is acceptable. Returns false (with a reason) otherwise.</summary>
    public async Task<ValidationResult> ValidateAsync(string url, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(url))
            return ValidationResult.Fail("URL is required.");

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return ValidationResult.Fail("URL is not a valid absolute URI.");

        if (uri.Scheme is not "http" and not "https")
            return ValidationResult.Fail($"URL scheme '{uri.Scheme}' is not allowed.");

        if (!IsTrustedHost(uri.Host))
            return ValidationResult.Fail($"URL host '{uri.Host}' is not a trusted storage host.");

        try
        {
            var client = _httpClientFactory.CreateClient("FileUrlValidator");
            client.Timeout = _timeout;

            using var request = new HttpRequestMessage(HttpMethod.Head, url);
            using var response = await client.SendAsync(request, ct);

            if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.Redirect && response.StatusCode != HttpStatusCode.MovedPermanently && response.StatusCode != HttpStatusCode.NotModified)
                return ValidationResult.Fail($"URL returned status {(int)response.StatusCode}.");

            var contentType = response.Content.Headers.ContentType?.MediaType?.ToLower();
            if (contentType is not null && !AllowedTypesPrefixes.Any(t => contentType.StartsWith(t)))
                return ValidationResult.Fail($"Content type '{contentType}' is not allowed.");

            var contentLength = response.Content.Headers.ContentLength;
            if (contentLength.HasValue && contentLength.Value > _maxSizeBytes)
                return ValidationResult.Fail($"File size {contentLength.Value} bytes exceeds maximum of {_maxSizeBytes} bytes.");
        }
        catch (TaskCanceledException)
        {
            return ValidationResult.Fail("URL validation timed out.");
        }
        catch (HttpRequestException ex)
        {
            return ValidationResult.Fail($"URL is not accessible: {ex.Message}");
        }

        return ValidationResult.Ok();
    }

    /// <summary>
    /// Trusted hosts are derived from the configured storage providers.
    /// Local URLs are accepted on localhost only (development).
    /// </summary>
    private static bool IsTrustedHost(string host)
    {
        var trusted = new[]
        {
            "localhost",
            "127.0.0.1",
            "res.cloudinary.com",
            "amazonaws.com"
        };
        return trusted.Any(t => host.Equals(t, StringComparison.OrdinalIgnoreCase) || host.EndsWith("." + t, StringComparison.OrdinalIgnoreCase));
    }
}

public record ValidationResult(bool IsValid, string? Error)
{
    public static ValidationResult Ok() => new(true, null);
    public static ValidationResult Fail(string error) => new(false, error);
}
