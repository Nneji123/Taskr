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

    /// <summary>Shortcut to create a 201 Created response.</summary>
    protected IActionResult CreatedResult<T>(T data, string message = "Created successfully")
        => StatusCode(201, ApiResponse<T>.Ok(data, message));

    /// <summary>Shortcut to create a 200 OK response for delete operations.</summary>
    protected IActionResult DeletedResult(string message = "Deleted successfully")
        => Ok(ApiResponse<object?>.Ok(null, message));
}
