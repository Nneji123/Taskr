using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using API.Common;
using API.Common.Files;
using API.Common.Storage;

namespace API.Common.Files.Controllers;

[Authorize]
[Route("v1/files")]
[EnableRateLimiting("write-strict")]
public class FilesController(IStorageService storage, ICurrentUser currentUser, ILogger<FilesController> logger) : BaseController(currentUser)
{
    private static readonly string[] AllowedContentTypes =
    [
        "image/jpeg", "image/png", "image/gif", "image/webp",
        "application/pdf"
    ];
    private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxFileSize)]
    [ProducesResponseType(typeof(ApiResponse<FileResponse>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Upload([FromForm] IFormFileCollection files, CancellationToken ct)
    {
        if (files is null || files.Count == 0)
            return Ok(ApiResponse.Fail("At least one file is required in the 'files' field."));

        var results = new List<FileResponse>(files.Count);
        foreach (var file in files)
        {
            if (file.Length == 0)
                return Ok(ApiResponse.Fail($"File '{file.FileName}' is empty."));

            if (file.Length > MaxFileSize)
                return Ok(ApiResponse.Fail($"File '{file.FileName}' exceeds 10MB limit."));

            if (!AllowedContentTypes.Contains(file.ContentType))
                return Ok(ApiResponse.Fail($"File '{file.FileName}' has unsupported content type '{file.ContentType}'. Allowed: {string.Join(", ", AllowedContentTypes)}"));

            await using var stream = file.OpenReadStream();
            var url = await storage.UploadAsync(stream, file.FileName, file.ContentType, ct);

            results.Add(new FileResponse
            {
                Id = Guid.NewGuid().ToString("N"),
                Url = url,
                OriginalFilename = file.FileName,
                FileSize = file.Length,
                ContentType = file.ContentType
            });

            logger.LogInformation("Uploaded {Filename} ({Size} bytes) to {Url}", file.FileName, file.Length, url);
        }

        var data = results.Count == 1 ? (object)results[0] : results;
        return StatusCode(201, ApiResponse<object>.Ok(data, $"Successfully uploaded {results.Count} file(s)."));
    }

    [HttpDelete]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete([FromQuery] string url, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(url))
            return Ok(ApiResponse.Fail("Query parameter 'url' is required."));

        await storage.DeleteAsync(url, ct);
        return OkResult<object?>(null, "File deleted successfully.");
    }
}
