using System.Text.Json.Serialization;
using API.Common;
using API.Features.Tasks.Models;

namespace API.Features.Tasks.DTOs;

/// <summary>Payload for <c>POST /v1/projects/{projectId}/tasks</c>.</summary>
public class CreateTaskRequest
{
    /// <summary>Short title describing the task. Required.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Optional longer description or notes for the task.</summary>
    public string? Description { get; set; }

    /// <summary>Optional list of attachment URLs uploaded via <c>POST /v1/files</c>.</summary>
    public List<string>? Attachments { get; set; }

    /// <summary>Optional priority. Defaults to <c>Medium</c> on the server.</summary>
    public TaskPriority? Priority { get; set; }

    /// <summary>Optional due date (ISO 8601, UTC).</summary>
    public DateTime? DueDate { get; set; }

    /// <summary>Optional user id to assign the task to. Must be a known user.</summary>
    public Guid? AssigneeId { get; set; }

    /// <summary>Optional free-form key/value metadata stored on the task.</summary>
    public Dictionary<string, object?>? Metadata { get; set; }
}

/// <summary>Payload for <c>PATCH /v1/tasks/{id}</c>. All fields are optional.</summary>
public class UpdateTaskRequest
{
    /// <summary>New title.</summary>
    public string? Title { get; set; }

    /// <summary>New description. Pass an empty string to clear.</summary>
    public string? Description { get; set; }

    /// <summary>Replacement attachment URL list. Pass an empty list to clear.</summary>
    public List<string>? Attachments { get; set; }

    /// <summary>New status. Setting to <c>Done</c> records <c>completedAt</c>.</summary>
    public TaskItemStatus? Status { get; set; }

    /// <summary>New priority.</summary>
    public TaskPriority? Priority { get; set; }

    /// <summary>New due date. Pass null to clear.</summary>
    public DateTime? DueDate { get; set; }

    /// <summary>New assignee. Pass null to unassign.</summary>
    public Guid? AssigneeId { get; set; }

    /// <summary>Replacement metadata. Pass an empty object to clear.</summary>
    public Dictionary<string, object?>? Metadata { get; set; }
}

/// <summary>Query string for <c>GET /v1/projects/{projectId}/tasks</c>.</summary>
public class TaskListQuery : PagedRequest
{
    /// <summary>Filter by status (e.g. <c>Todo</c>, <c>InProgress</c>, <c>Done</c>).</summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>Filter by priority (e.g. <c>Low</c>, <c>Medium</c>, <c>High</c>, <c>Urgent</c>).</summary>
    [JsonPropertyName("priority")]
    public string? Priority { get; set; }

    /// <summary>Filter by assignee user id.</summary>
    [JsonPropertyName("assigneeId")]
    public Guid? AssigneeId { get; set; }

    /// <summary>Filter tasks due on/after this date (ISO 8601).</summary>
    [JsonPropertyName("dueDateFrom")]
    public DateTime? DueDateFrom { get; set; }

    /// <summary>Filter tasks due on/before this date (ISO 8601).</summary>
    [JsonPropertyName("dueDateTo")]
    public DateTime? DueDateTo { get; set; }
}
