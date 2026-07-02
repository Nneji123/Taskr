using API.Common.Files;
using API.Features.Tasks.Models;

namespace API.Features.Tasks.DTOs;

/// <summary>Task resource returned by task endpoints.</summary>
public class TaskResponse
{
    /// <summary>Unique task identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Short title describing the task.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Optional longer description or notes.</summary>
    public string? Description { get; set; }

    /// <summary>Attachment files, each resolved with a fresh signed URL.</summary>
    public List<FileResponse> Attachments { get; set; } = [];

    /// <summary>Identifier of the project the task belongs to.</summary>
    public Guid ProjectId { get; set; }

    /// <summary>Display name of the project the task belongs to.</summary>
    public string? ProjectName { get; set; }

    /// <summary>Identifier of the assigned user, if any.</summary>
    public Guid? AssigneeId { get; set; }

    /// <summary>Display name of the assigned user, if any.</summary>
    public string? AssigneeName { get; set; }

    /// <summary>Current status of the task.</summary>
    public TaskItemStatus Status { get; set; }

    /// <summary>Current priority of the task.</summary>
    public TaskPriority Priority { get; set; }

    /// <summary>Optional due date (ISO 8601, UTC).</summary>
    public DateTime? DueDate { get; set; }

    /// <summary>Timestamp (UTC) the task was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Timestamp (UTC) the task was last updated.</summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>Timestamp (UTC) the task was moved to <c>Done</c>, if ever.</summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>Free-form key/value metadata attached to the task.</summary>
    public Dictionary<string, object?> Metadata { get; set; } = [];
}
