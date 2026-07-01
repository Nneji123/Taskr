using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace API.Common.Files;

/// <summary>Response for a single uploaded file.</summary>
public class FileResponse
{
    /// <summary>Server-generated identifier for the file record.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Publicly accessible URL where the file can be fetched. Store this on the owning resource.</summary>
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
