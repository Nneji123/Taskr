using API.Common;
using API.Features.Tasks.Models;

namespace API.Features.Tasks.DTOs;

public class CreateTaskRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<string>? Attachments { get; set; }
    public TaskPriority? Priority { get; set; }
    public DateTime? DueDate { get; set; }
    public Guid? AssigneeId { get; set; }
    public Dictionary<string, object?>? Metadata { get; set; }
}

public class UpdateTaskRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public List<string>? Attachments { get; set; }
    public TaskItemStatus? Status { get; set; }
    public TaskPriority? Priority { get; set; }
    public DateTime? DueDate { get; set; }
    public Guid? AssigneeId { get; set; }
    public Dictionary<string, object?>? Metadata { get; set; }
}

public class TaskListQuery : PagedRequest
{
    public string? Status { get; set; }
    public string? Priority { get; set; }
    public Guid? AssigneeId { get; set; }
    public DateTime? DueDateFrom { get; set; }
    public DateTime? DueDateTo { get; set; }
}
