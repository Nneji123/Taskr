using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace API.Common.Files;

public class FileResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("originalFilename")]
    public string OriginalFilename { get; set; } = string.Empty;

    [JsonPropertyName("fileSize")]
    public long FileSize { get; set; }

    [JsonPropertyName("fileSizeDisplay")]
    public string FileSizeDisplay => FormatBytes(FileSize);

    [JsonPropertyName("contentType")]
    public string ContentType { get; set; } = string.Empty;

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
