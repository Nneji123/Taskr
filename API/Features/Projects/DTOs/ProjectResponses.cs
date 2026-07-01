namespace API.Features.Projects.DTOs;

/// <summary>Project resource returned by project endpoints.</summary>
public class ProjectResponse
{
    /// <summary>Unique project identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Display name of the project.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Longer description of the project, if any.</summary>
    public string? Description { get; set; }

    /// <summary>Optional cover image URL.</summary>
    public string? CoverImageUrl { get; set; }

    /// <summary>Identifier of the user who owns the project.</summary>
    public Guid OwnerId { get; set; }

    /// <summary>Timestamp (UTC) the project was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Timestamp (UTC) the project was last updated.</summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>Free-form key/value metadata attached to the project.</summary>
    public Dictionary<string, object?> Metadata { get; set; } = [];
}
