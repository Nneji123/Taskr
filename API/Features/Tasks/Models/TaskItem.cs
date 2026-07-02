using API.Common;
using API.Features.Auth.Models;
using API.Features.Projects.Models;

namespace API.Features.Tasks.Models;

/// <summary>
/// A unit of work that belongs to a project. Tasks have a status, priority,
/// optional due date, and optional assignee.
/// </summary>
public class TaskItem : BaseModel
{
    /// <summary>Short title describing the task.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Optional longer description or notes.</summary>
    public string? Description { get; set; }

    /// <summary>Attachments linked to this task via <see cref="TaskAttachment"/> join records.</summary>
    public ICollection<TaskAttachment> Attachments { get; set; } = [];

    /// <summary>Identifier of the project the task belongs to.</summary>
    public Guid ProjectId { get; set; }

    /// <summary>Navigation property for the owning project.</summary>
    public Project Project { get; set; } = null!;

    /// <summary>Identifier of the user the task is assigned to, if any.</summary>
    public Guid? AssigneeId { get; set; }

    /// <summary>Navigation property for the assigned user, if any.</summary>
    public User? Assignee { get; set; }

    /// <summary>Current status of the task. Defaults to <c>Todo</c>.</summary>
    public TaskItemStatus Status { get; set; } = TaskItemStatus.Todo;

    /// <summary>Current priority of the task. Defaults to <c>Medium</c>.</summary>
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;

    /// <summary>Optional due date (UTC).</summary>
    public DateTime? DueDate { get; set; }

    /// <summary>Timestamp (UTC) the task was moved to <c>Done</c>, if ever.</summary>
    public DateTime? CompletedAt { get; set; }
}

/// <summary>Lifecycle status of a task.</summary>
public enum TaskItemStatus
{
    /// <summary>Task is created but not yet started.</summary>
    Todo,

    /// <summary>Task is actively being worked on.</summary>
    InProgress,

    /// <summary>Task is awaiting review or sign-off.</summary>
    InReview,

    /// <summary>Task is finished and accepted.</summary>
    Done,

    /// <summary>Task is closed without completion (no longer actionable).</summary>
    Archived
}

/// <summary>Priority level of a task. Used for sorting and filtering.</summary>
public enum TaskPriority
{
    /// <summary>Low urgency. Can be deferred.</summary>
    Low,

    /// <summary>Normal urgency. Default.</summary>
    Medium,

    /// <summary>High urgency. Should be addressed soon.</summary>
    High,

    /// <summary>Critical urgency. Requires immediate attention.</summary>
    Urgent
}
