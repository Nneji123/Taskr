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
    [ProducesResponseType(typeof(PagedApiResponse<TaskResponse>), StatusCodes.Status200OK)]
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

    /// <summary>Add an attachment to a task.</summary>
    /// <remarks>
    /// Links an uploaded file (by its database <c>fileRecordId</c>) to the task.
    /// The file must have been uploaded by the calling user via <c>POST /v1/files</c>.
    /// </remarks>
    [HttpPost("v1/tasks/{id:guid}/attachments")]
    [EnableRateLimiting("write-strict")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> AddAttachment(Guid id, [FromBody] AddTaskAttachmentRequest request, CancellationToken ct)
    {
        await tasksService.AddAttachmentAsync(CurrentUser.Id, id, request.FileRecordId, ct);
        return OkResult<object?>(null, "Attachment added.");
    }

    /// <summary>Remove an attachment from a task by its file record id.</summary>
    /// <remarks>Permanently removes the attachment link. Does not delete the underlying file.</remarks>
    [HttpDelete("v1/tasks/{taskId:guid}/attachments/{fileRecordId:guid}")]
    [EnableRateLimiting("write-strict")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> RemoveAttachment(Guid taskId, Guid fileRecordId, CancellationToken ct)
    {
        await tasksService.RemoveAttachmentAsync(CurrentUser.Id, taskId, fileRecordId, ct);
        return OkResult<object?>(null, "Attachment removed.");
    }
}
