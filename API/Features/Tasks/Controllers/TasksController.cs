using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using API.Common;
using API.Features.Tasks.DTOs;
using API.Features.Tasks.Services;

namespace API.Features.Tasks.Controllers;

/// <summary>
/// Task endpoints. Tasks are scoped to a project. All operations are
/// owner-scoped through the project hierarchy.
/// </summary>
[Authorize]
[EnableRateLimiting("api-default")]
public class TasksController(ITasksService tasksService, ICurrentUser currentUser) : BaseController(currentUser)
{
    /// <summary>List tasks for a project.</summary>
    /// <remarks>
    /// Returns a paginated list of tasks belonging to the project identified
    /// by <paramref name="projectId"/>. Supports status, priority, assignee,
    /// and due-date filtering.
    /// </remarks>
    [HttpGet("v1/projects/{projectId:guid}/tasks")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<TaskResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> List(Guid projectId, [FromQuery] TaskListQuery query, CancellationToken ct)
    {
        var result = await tasksService.ListAsync(CurrentUser.Id, projectId, query, ct);
        return PaginatedResult(result);
    }

    /// <summary>Create a task in a project.</summary>
    /// <remarks>
    /// Creates a new task under the project identified by
    /// <paramref name="projectId"/>. The caller must own the project.
    /// </remarks>
    [HttpPost("v1/projects/{projectId:guid}/tasks")]
    [EnableRateLimiting("write-strict")]
    [ProducesResponseType(typeof(ApiResponse<TaskResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Create(Guid projectId, [FromBody] CreateTaskRequest request, CancellationToken ct)
    {
        var result = await tasksService.CreateAsync(CurrentUser.Id, projectId, request, ct);
        return CreatedResult(result, "Task created");
    }

    /// <summary>Get a task by id.</summary>
    /// <remarks>Returns the task identified by <paramref name="id"/> if the caller owns its project.</remarks>
    [HttpGet("v1/tasks/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<TaskResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await tasksService.GetByIdAsync(CurrentUser.Id, id, ct);
        return OkResult(result);
    }

    /// <summary>Update a task.</summary>
    /// <remarks>
    /// Partial update. Only the supplied fields are modified. Moving a task
    /// to status <c>Done</c> sets <c>completedAt</c>; moving it out clears it.
    /// </remarks>
    [HttpPatch("v1/tasks/{id:guid}")]
    [EnableRateLimiting("write-strict")]
    [ProducesResponseType(typeof(ApiResponse<TaskResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTaskRequest request, CancellationToken ct)
    {
        var result = await tasksService.UpdateAsync(CurrentUser.Id, id, request, ct);
        return OkResult(result);
    }

    /// <summary>Delete a task.</summary>
    /// <remarks>Permanently deletes the task. Project owner only.</remarks>
    [HttpDelete("v1/tasks/{id:guid}")]
    [EnableRateLimiting("write-strict")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await tasksService.DeleteAsync(CurrentUser.Id, id, ct);
        return DeletedResult("Task deleted");
    }
}
