using API.Common;

namespace API.Features.Projects.DTOs;

public class CreateProjectRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? CoverImageUrl { get; set; }
}

public class UpdateProjectRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? CoverImageUrl { get; set; }
    public Dictionary<string, object?>? Metadata { get; set; }
}

public class ProjectListQuery : PagedRequest
{
}
