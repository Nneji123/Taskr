using API.Common;
using API.Features.Auth.Models;
using API.Features.Tasks.Models;

namespace API.Features.Projects.Models;

public class Project : BaseModel
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? CoverImageUrl { get; set; }
    public Guid OwnerId { get; set; }
    public User Owner { get; set; } = null!;
    public ICollection<TaskItem> Tasks { get; set; } = [];
}
