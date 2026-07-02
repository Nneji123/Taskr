using API.Common;

namespace API.Features.Projects.DTOs;

/// <summary>Payload for <c>POST /v1/projects</c>.</summary>
public class CreateProjectRequest
{
    /// <summary>Display name of the project. Required.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional longer description shown on the project page.</summary>
    public string? Description { get; set; }

    /// <summary>Optional cover image file id. Upload via <c>POST /v1/files</c> first, then pass the returned <c>id</c>.</summary>
    public Guid? CoverImageId { get; set; }

    /// <summary>Optional free-form key/value metadata stored on the project.</summary>
    public Dictionary<string, object?>? Metadata { get; set; }
}

/// <summary>Payload for <c>PATCH /v1/projects/{id}</c>. All fields are optional.</summary>
public class UpdateProjectRequest
{
    /// <summary>New project name. Omit to leave unchanged.</summary>
    public string? Name { get; set; }

    /// <summary>New description. Pass an empty string to clear.</summary>
    public string? Description { get; set; }

    /// <summary>New cover image file id. Pass <c>null</c> to clear.</summary>
    public Guid? CoverImageId { get; set; }

    /// <summary>Replacement metadata. Pass an empty object to clear.</summary>
    public Dictionary<string, object?>? Metadata { get; set; }
}

/// <summary>Query string for <c>GET /v1/projects</c>. Inherits shared paging parameters.</summary>
public class ProjectListQuery : PagedRequest
{
}
