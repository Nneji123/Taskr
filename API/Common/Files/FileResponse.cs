using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace API.Common.Files;

/// <summary>Response for a single uploaded file.</summary>
public class FileResponse
{
    /// <summary>Database identifier for the file record.</summary>
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    /// <summary>Storage key (S3 object key). Use this to request new signed URLs via <c>GET /v1/files/signed-url</c>.</summary>
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    /// <summary>Time-limited signed URL granting read access to the file.</summary>
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    /// <summary>The filename provided by the client at upload time.</summary>
    [JsonPropertyName("originalFilename")]
    public string OriginalFilename { get; set; } = string.Empty;

    /// <summary>File size in bytes.</summary>
    [JsonPropertyName("fileSize")]
    public long FileSize { get; set; }

    /// <summary>Human-readable file size (e.g. <c>12.1 KB</c>).</summary>
    [JsonPropertyName("fileSizeDisplay")]
    public string FileSizeDisplay => FormatBytes(FileSize);

    /// <summary>MIME type of the uploaded file.</summary>
    [JsonPropertyName("contentType")]
    public string ContentType { get; set; } = string.Empty;

    /// <summary>Timestamp (UTC) the file was uploaded.</summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    private static string FormatBytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB"];
        double size = bytes;
        int i = 0;
        while (size >= 1024 && i < units.Length - 1)
        {
            size /= 1024;
            i++;
        }
        return $"{size:0.#} {units[i]}";
    }
}

/// <summary>Response payload for a generated pre-signed URL.</summary>
public class SignedUrlResponse
{
    /// <summary>The pre-signed URL granting time-limited read access.</summary>
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    /// <summary>The storage key the URL was generated for.</summary>
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    /// <summary>Actual TTL the server applied, in seconds.</summary>
    [JsonPropertyName("ttlSeconds")]
    public int TtlSeconds { get; set; }

    /// <summary>Timestamp (UTC) at which the URL stops being valid.</summary>
    [JsonPropertyName("expiresAt")]
    public DateTime ExpiresAt { get; set; }
}
