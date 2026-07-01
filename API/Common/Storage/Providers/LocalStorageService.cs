using Microsoft.Extensions.Options;
using API.Options;

namespace API.Common.Storage.Providers;

/// <summary>
/// Stores files on the local filesystem. Intended for development only —
/// files are lost on container restart and not suitable for production.
/// </summary>
public class LocalStorageService(IOptions<StorageOptions> options, IWebHostEnvironment env) : IStorageService
{
    private readonly LocalStorageSettings _settings = options.Value.Local;

    public Task<string> UploadAsync(Stream content, string fileName, string contentType, CancellationToken ct = default)
    {
        var uploadsDir = Path.Combine(env.ContentRootPath, _settings.BasePath);
        Directory.CreateDirectory(uploadsDir);

        var uniqueName = $"{Guid.NewGuid():N}_{fileName}";
        var filePath = Path.Combine(uploadsDir, uniqueName);

        using var fileStream = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, useAsync: true);
        content.CopyTo(fileStream);

        var url = $"{_settings.BaseUrl}/{uniqueName}";
        return Task.FromResult(url);
    }

    public Task DeleteAsync(string url, CancellationToken ct = default)
    {
        var fileName = Path.GetFileName(url);
        if (string.IsNullOrEmpty(fileName)) return Task.CompletedTask;

        var filePath = Path.Combine(env.ContentRootPath, _settings.BasePath, fileName);
        if (File.Exists(filePath))
            File.Delete(filePath);

        return Task.CompletedTask;
    }

    public string GetUrl(string key) => $"{_settings.BaseUrl}/{key}";
}
