using Microsoft.AspNetCore.Mvc;

namespace API.Common;

/// <summary>
/// Base controller for all API feature controllers. Provides common infrastructure
/// like the current user, pagination helpers, and response formatting.
/// </summary>
[ApiController]
public abstract class BaseController(ICurrentUser currentUser) : ControllerBase
{
    /// <summary>The currently authenticated user (empty guid for anonymous endpoints).</summary>
    protected ICurrentUser CurrentUser { get; } = currentUser;

    /// <summary>Shortcut to create a 200 OK response with the standard envelope.</summary>
    protected IActionResult OkResult<T>(T data, string message = "Operation successful")
        => Ok(ApiResponse<T>.Ok(data, message));

    /// <summary>Shortcut to create a 200 OK paginated response. <c>data</c> is the items list, <c>meta</c> holds pagination info.</summary>
    protected IActionResult PaginatedResult<T>(PagedResult<T> result, string message = "Operation successful")
    {
        var meta = new PaginationMeta
        {
            Page = result.Page,
            PageSize = result.PageSize,
            TotalCount = result.TotalCount,
            TotalPages = result.TotalPages,
            HasNext = result.HasNext,
            HasPrevious = result.HasPrevious
        };
        return Ok(new PagedApiResponse<T>
        {
            Success = true,
            Message = message,
            Data = result.Items,
            Meta = meta,
            Status = 200
        });
    }

    /// <summary>Shortcut to create a 201 Created response.</summary>
    protected IActionResult CreatedResult<T>(T data, string message = "Created successfully")
        => StatusCode(201, ApiResponse<T>.Ok(data, message));

    /// <summary>Shortcut to create a 200 OK response for delete operations.</summary>
    protected IActionResult DeletedResult(string message = "Deleted successfully")
        => Ok(ApiResponse<object?>.Ok(null, message));

    /// <summary>Shortcut to create a 400 Bad Request response with the standard envelope.</summary>
    protected IActionResult BadRequestResult(string message, object? errors = null)
        => BadRequest(ApiResponse.Fail(message, errors));
}
