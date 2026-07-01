using API.Features.Tasks.Models;

namespace API.Features.Tasks.DTOs;

public class TaskResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<string> Attachments { get; set; } = [];
    public Guid ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public Guid? AssigneeId { get; set; }
    public string? AssigneeName { get; set; }
    public TaskItemStatus Status { get; set; }
    public TaskPriority Priority { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public Dictionary<string, object?> Metadata { get; set; } = [];
}
