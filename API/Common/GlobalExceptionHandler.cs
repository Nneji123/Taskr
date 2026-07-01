using System.Text.Json;
using API.Common;
using Microsoft.AspNetCore.Diagnostics;

using FluentValidation;
using Sentry;

namespace API.Common;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken ct)
    {
        var (statusCode, message, errors) = exception switch
        {
            ValidationException ve => (
                StatusCodes.Status422UnprocessableEntity,
                "Validation failed",
                ve.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage })
            ),
            ApiException ae => (ae.StatusCode, ae.Message, ae.Errors),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.", null)
        };

        if (statusCode >= 500)
        {
            var eventId = SentrySdk.CaptureException(exception);
            logger.LogError(exception, "Unhandled exception. Sentry: {EventId}", eventId);
        }

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/json";

        var response = ApiResponse.Fail(message, errors);
        await httpContext.Response.WriteAsync(JsonSerializer.Serialize(response), ct);

        return true;
    }
}
