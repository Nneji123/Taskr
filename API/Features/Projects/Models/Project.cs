using API.Common;
using API.Common.Files.Models;
using API.Features.Auth.Models;
using API.Features.Tasks.Models;

namespace API.Features.Projects.Models;

/// <summary>
/// A project is a container for tasks. Each project has a single owner and
/// zero or more tasks.
/// </summary>
public class Project : BaseModel
{
    /// <summary>Display name of the project.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional longer description.</summary>
    public string? Description { get; set; }

    /// <summary>Optional cover image file record. Set via <c>POST /v1/files</c>, then pass the returned <c>id</c> on creation or update.</summary>
    public Guid? CoverImageId { get; set; }

    /// <summary>Navigation property for the cover image, if any.</summary>
    public FileRecord? CoverImage { get; set; }

    /// <summary>Identifier of the owning user.</summary>
    public Guid OwnerId { get; set; }

    /// <summary>Navigation property for the owning user.</summary>
    public User Owner { get; set; } = null!;

    /// <summary>Tasks that belong to this project.</summary>
    public ICollection<TaskItem> Tasks { get; set; } = [];
}
