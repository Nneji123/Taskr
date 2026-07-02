using API.Common;
using API.Common.Files.Models;

namespace API.Features.Tasks.Models;

/// <summary>
/// Join entity linking a task to an uploaded file. A task can have zero or
/// more attachments; each attachment references a <see cref="FileRecord"/>.
/// </summary>
public class TaskAttachment : BaseModel
{
    /// <summary>Identifier of the owning task.</summary>
    public Guid TaskId { get; set; }

    /// <summary>Navigation property for the owning task.</summary>
    public TaskItem Task { get; set; } = null!;

    /// <summary>Identifier of the referenced file record.</summary>
    public Guid FileRecordId { get; set; }

    /// <summary>Navigation property for the referenced file.</summary>
    public FileRecord FileRecord { get; set; } = null!;
}
