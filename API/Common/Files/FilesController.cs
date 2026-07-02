using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using API.Common;
using API.Common.Files;
using API.Common.Files.Models;
using API.Common.Storage;
using API.Data;

namespace API.Common.Files.Controllers;

/// <summary>
/// File upload and deletion endpoints. Uploaded files are stored on the
/// configured storage provider (S3, Cloudinary, or local). Every upload
/// creates a <see cref="FileRecord"/> in the database so that other resources
/// can reference files by their database ID.
/// </summary>
[Authorize]
[Route("v1/files")]
[EnableRateLimiting("write-strict")]
public class FilesController(IStorageService storage, AppDbContext db, ICurrentUser currentUser, ILogger<FilesController> logger) : BaseController(currentUser)
{
    private static readonly string[] AllowedContentTypes =
    [
        "image/jpeg", "image/png", "image/gif", "image/webp",
        "application/pdf"
    ];
    private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB

    /// <summary>Upload one or more files.</summary>
    /// <remarks>
    /// Accepts a <c>multipart/form-data</c> body with one or more files in the
    /// <c>files</c> field. Each file must be one of the allowed content types
    /// and no larger than 10 MB. The returned <c>id</c> is the database
    /// identifier that should be stored on resources (avatar, cover image,
    /// task attachments, etc.).
    /// </remarks>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxFileSize)]
    [ProducesResponseType(typeof(ApiResponse<FileResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status415UnsupportedMediaType)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status429TooManyRequests)]
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
            var key = ExtractKeyFromUrl(url);

            var record = new FileRecord
            {
                Key = key,
                OriginalFilename = file.FileName,
                ContentType = file.ContentType,
                FileSize = file.Length,
                UploadedByUserId = CurrentUser.Id
            };
            db.FileRecords.Add(record);
            await db.SaveChangesAsync(ct);

            results.Add(new FileResponse
            {
                Id = record.Id,
                Key = key,
                Url = url,
                OriginalFilename = file.FileName,
                FileSize = file.Length,
                ContentType = file.ContentType,
                CreatedAt = record.CreatedAt
            });

            logger.LogInformation("Uploaded {Filename} ({Size} bytes) — FileRecord {Id}", file.FileName, file.Length, record.Id);
        }

        var data = results.Count == 1 ? (object)results[0] : results;
        return StatusCode(201, ApiResponse<object>.Ok(data, $"Successfully uploaded {results.Count} file(s)."));
    }

    /// <summary>Delete a previously uploaded file by its database ID.</summary>
    /// <remarks>
    /// Removes the underlying object from the configured storage provider and
    /// deletes the <see cref="FileRecord"/> from the database. Only the
    /// uploading user may delete their own files.
    /// </remarks>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var record = await db.FileRecords.FirstOrDefaultAsync(f => f.Id == id, ct)
            ?? throw new NotFoundException("File", id);

        if (record.UploadedByUserId != CurrentUser.Id)
            throw new ForbiddenException();

        await storage.DeleteAsync(record.Key, ct);
        db.FileRecords.Remove(record);
        await db.SaveChangesAsync(ct);

        return OkResult<object?>(null, "File deleted successfully.");
    }

    private const int MinSignedUrlTtlSeconds = 30;
    private const int MaxSignedUrlTtlSeconds = 3600;
    private const int DefaultSignedUrlTtlSeconds = 300;

    /// <summary>Generate a short-lived pre-signed URL for a stored object.</summary>
    /// <remarks>
    /// Returns a time-limited URL that grants read access to the object identified
    /// by <c>key</c>. Use this when files are stored privately and the client needs
    /// to fetch them on demand (e.g. to render in a browser).
    ///
    /// The requested <c>ttlSeconds</c> is clamped to the range
    /// <c>30 ≤ ttl ≤ 3600</c> seconds; the response always
    /// reports the actual TTL that was applied. The default is 300 seconds (5 min).
    /// </remarks>
    [HttpGet("signed-url")]
    [EnableRateLimiting("api-default")]
    [ProducesResponseType(typeof(ApiResponse<SignedUrlResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status429TooManyRequests)]
    public IActionResult GetSignedUrl([FromQuery] string? key, [FromQuery] int? ttlSeconds)
    {
        if (string.IsNullOrWhiteSpace(key))
            return BadRequestResult("Query parameter 'key' is required.");

        if (key.Contains("..", StringComparison.Ordinal) || key.StartsWith('/'))
            return BadRequestResult("Query parameter 'key' is invalid.");

        var requested = ttlSeconds ?? DefaultSignedUrlTtlSeconds;
        var clamped = Math.Clamp(requested, MinSignedUrlTtlSeconds, MaxSignedUrlTtlSeconds);
        var ttl = TimeSpan.FromSeconds(clamped);

        var url = storage.GetSignedUrl(key, ttl);

        var response = new SignedUrlResponse
        {
            Url = url,
            Key = key,
            TtlSeconds = clamped,
            ExpiresAt = DateTime.UtcNow.AddSeconds(clamped)
        };

        return OkResult(response, "Signed URL generated successfully.");
    }

    private static string ExtractKeyFromUrl(string url)
    {
        var uri = new Uri(url);
        var segments = uri.AbsolutePath.TrimStart('/').Split('/');
        return segments.Length > 0 ? segments[^1] : url;
    }
}
