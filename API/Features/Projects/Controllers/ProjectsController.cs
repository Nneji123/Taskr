using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using API.Common;
using API.Features.Projects.DTOs;
using API.Features.Projects.Services;

namespace API.Features.Projects.Controllers;

/// <summary>
/// Project endpoints. All operations are owner-scoped: a user can only
/// read, update, or delete projects they own.
/// </summary>
[Authorize]
[Route("v1/projects")]
[EnableRateLimiting("api-default")]
public class ProjectsController(IProjectsService projectsService, ICurrentUser currentUser) : BaseController(currentUser)
{
    /// <summary>List the authenticated user's projects.</summary>
    /// <remarks>
    /// Returns a paginated list of projects owned by the caller. Supports
    /// search, date filtering, and sorting (see query parameters).
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ProjectResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> List([FromQuery] ProjectListQuery query, CancellationToken ct)
    {
        var result = await projectsService.ListAsync(CurrentUser.Id, query, ct);
        return OkResult(result);
    }

    /// <summary>Create a new project.</summary>
    /// <remarks>
    /// Creates a project owned by the authenticated user. Returns the new
    /// project resource.
    /// </remarks>
    [HttpPost]
    [EnableRateLimiting("write-strict")]
    [ProducesResponseType(typeof(ApiResponse<ProjectResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Create([FromBody] CreateProjectRequest request, CancellationToken ct)
    {
        var result = await projectsService.CreateAsync(CurrentUser.Id, request, ct);
        return CreatedResult(result, "Project created");
    }

    /// <summary>Get a single project by id.</summary>
    /// <remarks>Returns the project identified by <paramref name="id"/> if the caller owns it.</remarks>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ProjectResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await projectsService.GetByIdAsync(CurrentUser.Id, id, ct);
        return OkResult(result);
    }

    /// <summary>Update a project.</summary>
    /// <remarks>
    /// Partial update. Only the supplied fields are modified. The caller must
    /// own the project.
    /// </remarks>
    [HttpPatch("{id:guid}")]
    [EnableRateLimiting("write-strict")]
    [ProducesResponseType(typeof(ApiResponse<ProjectResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProjectRequest request, CancellationToken ct)
    {
        var result = await projectsService.UpdateAsync(CurrentUser.Id, id, request, ct);
        return OkResult(result);
    }

    /// <summary>Delete a project.</summary>
    /// <remarks>Permanently deletes the project and cascades to its tasks. Owner only.</remarks>
    [HttpDelete("{id:guid}")]
    [EnableRateLimiting("write-strict")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await projectsService.DeleteAsync(CurrentUser.Id, id, ct);
        return DeletedResult("Project deleted");
    }
}
