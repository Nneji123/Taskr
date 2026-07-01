using API.Common;
using API.Features.Auth.Models;
using API.Features.Projects.Models;

namespace API.Features.Tasks.Models;

public class TaskItem : BaseModel
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<string> Attachments { get; set; } = [];
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;
    public Guid? AssigneeId { get; set; }
    public User? Assignee { get; set; }
    public TaskItemStatus Status { get; set; } = TaskItemStatus.Todo;
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public DateTime? DueDate { get; set; }
    public DateTime? CompletedAt { get; set; }
}
