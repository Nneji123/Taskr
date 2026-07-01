using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using API.Common;
using API.Features.Tasks.DTOs;
using API.Features.Tasks.Services;

namespace API.Features.Tasks.Controllers;

[Authorize]
[EnableRateLimiting("api-default")]
public class TasksController(ITasksService tasksService, ICurrentUser currentUser) : BaseController(currentUser)
{
    [HttpGet("v1/projects/{projectId:guid}/tasks")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<TaskResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(Guid projectId, [FromQuery] TaskListQuery query, CancellationToken ct)
    {
        var result = await tasksService.ListAsync(CurrentUser.Id, projectId, query, ct);
        return OkResult(result);
    }

    [HttpPost("v1/projects/{projectId:guid}/tasks")]
    [EnableRateLimiting("write-strict")]
    [ProducesResponseType(typeof(ApiResponse<TaskResponse>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(Guid projectId, [FromBody] CreateTaskRequest request, CancellationToken ct)
    {
        var result = await tasksService.CreateAsync(CurrentUser.Id, projectId, request, ct);
        return CreatedResult(result, "Task created");
    }

    [HttpGet("v1/tasks/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<TaskResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await tasksService.GetByIdAsync(CurrentUser.Id, id, ct);
        return OkResult(result);
    }

    [HttpPatch("v1/tasks/{id:guid}")]
    [EnableRateLimiting("write-strict")]
    [ProducesResponseType(typeof(ApiResponse<TaskResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTaskRequest request, CancellationToken ct)
    {
        var result = await tasksService.UpdateAsync(CurrentUser.Id, id, request, ct);
        return OkResult(result);
    }

    [HttpDelete("v1/tasks/{id:guid}")]
    [EnableRateLimiting("write-strict")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await tasksService.DeleteAsync(CurrentUser.Id, id, ct);
        return DeletedResult("Task deleted");
    }
}
