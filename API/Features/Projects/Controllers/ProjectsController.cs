using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using API.Common;
using API.Features.Projects.DTOs;
using API.Features.Projects.Services;

namespace API.Features.Projects.Controllers;

[Authorize]
[Route("v1/projects")]
[EnableRateLimiting("api-default")]
public class ProjectsController(IProjectsService projectsService, ICurrentUser currentUser) : BaseController(currentUser)
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ProjectResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] ProjectListQuery query, CancellationToken ct)
    {
        var result = await projectsService.ListAsync(CurrentUser.Id, query, ct);
        return OkResult(result);
    }

    [HttpPost]
    [EnableRateLimiting("write-strict")]
    [ProducesResponseType(typeof(ApiResponse<ProjectResponse>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateProjectRequest request, CancellationToken ct)
    {
        var result = await projectsService.CreateAsync(CurrentUser.Id, request, ct);
        return CreatedResult(result, "Project created");
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ProjectResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await projectsService.GetByIdAsync(CurrentUser.Id, id, ct);
        return OkResult(result);
    }

    [HttpPatch("{id:guid}")]
    [EnableRateLimiting("write-strict")]
    [ProducesResponseType(typeof(ApiResponse<ProjectResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProjectRequest request, CancellationToken ct)
    {
        var result = await projectsService.UpdateAsync(CurrentUser.Id, id, request, ct);
        return OkResult(result);
    }

    [HttpDelete("{id:guid}")]
    [EnableRateLimiting("write-strict")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await projectsService.DeleteAsync(CurrentUser.Id, id, ct);
        return DeletedResult("Project deleted");
    }
}
