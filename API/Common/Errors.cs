namespace API.Common;

public class ApiException(int statusCode, string message, object? errors = null) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
    public object? Errors { get; } = errors;
}

public class NotFoundException(string resource, object key)
    : ApiException(StatusCodes.Status404NotFound, $"Resource '{resource}' with key '{key}' was not found.");

public class ConflictException(string message)
    : ApiException(StatusCodes.Status409Conflict, message);

public class ForbiddenException(string message = "You do not have permission to access this resource.")
    : ApiException(StatusCodes.Status403Forbidden, message);

public class UnauthorizedException(string message = "Authentication is required.")
    : ApiException(StatusCodes.Status401Unauthorized, message);
