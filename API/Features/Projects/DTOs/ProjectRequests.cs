using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using API.Common;

namespace API.Features.Projects.DTOs;

/// <summary>Payload for <c>POST /v1/projects</c>.</summary>
public class CreateProjectRequest
{
    /// <summary>Display name of the project. Required.</summary>
    [Required]
    [StringLength(200, MinimumLength = 1)]
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional longer description shown on the project page.</summary>
    [StringLength(2000)]
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>Optional cover image file id. Upload via <c>POST /v1/files</c> first, then pass the returned <c>id</c>.</summary>
    [JsonPropertyName("coverImageId")]
    public Guid? CoverImageId { get; set; }

    /// <summary>Optional free-form key/value metadata stored on the project.</summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, object?>? Metadata { get; set; }
}

/// <summary>Payload for <c>PATCH /v1/projects/{id}</c>. All fields are optional. To clear the cover image, send <c>"coverImageId": null</c>.</summary>
public class UpdateProjectRequest
{
    /// <summary>New project name. Omit to leave unchanged.</summary>
    [StringLength(200, MinimumLength = 1)]
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>New description. Pass an empty string to clear.</summary>
    [StringLength(2000)]
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    private JsonElement _coverImageIdElement;
    private bool _coverImageIdPresent;

    /// <summary>Cover image file id. Pass <c>null</c> to clear. Omit the field to leave unchanged.</summary>
    [JsonPropertyName("coverImageId")]
    public JsonElement CoverImageIdElement
    {
        get => _coverImageIdElement;
        set
        {
            _coverImageIdElement = value;
            _coverImageIdPresent = true;
        }
    }

    /// <summary>Replacement metadata. Pass an empty object to clear.</summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, object?>? Metadata { get; set; }

    /// <summary>True if <c>coverImageId</c> was present in the request body (even if null).</summary>
    [JsonIgnore]
    public bool CoverImageIdPresent => _coverImageIdPresent;

    /// <summary>Parsed cover image id. <c>null</c> when missing or explicitly null.</summary>
    [JsonIgnore]
    public Guid? CoverImageId
    {
        get
        {
            if (!_coverImageIdPresent) return null;
            if (_coverImageIdElement.ValueKind == JsonValueKind.Null) return null;
            if (_coverImageIdElement.ValueKind == JsonValueKind.String && Guid.TryParse(_coverImageIdElement.GetString(), out var g)) return g;
            return null;
        }
    }
}

/// <summary>Query string for <c>GET /v1/projects</c>. Inherits shared paging parameters.</summary>
public class ProjectListQuery : PagedRequest
{
}
