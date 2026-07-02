using API.Common;
using API.Features.Auth.Models;

namespace API.Common.Files.Models;

/// <summary>
/// Persists metadata for an uploaded file. The S3 object key is the source of
/// truth for the binary; this record enables ID-based references from other
/// resources (User.AvatarId, Project.CoverImageId, TaskAttachment).
/// </summary>
public class FileRecord : BaseModel
{
    /// <summary>S3 object key for the uploaded file.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Original filename provided by the client at upload time.</summary>
    public string OriginalFilename { get; set; } = string.Empty;

    /// <summary>MIME content type of the file.</summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>File size in bytes.</summary>
    public long FileSize { get; set; }

    /// <summary>Identifier of the user who uploaded this file.</summary>
    public Guid UploadedByUserId { get; set; }

    /// <summary>Navigation property for the uploading user.</summary>
    public User UploadedByUser { get; set; } = null!;
}
