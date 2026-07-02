using System.Text.Json.Serialization;

namespace API.Common;

public class PaginationMeta
{
    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; }

    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }

    [JsonPropertyName("totalPages")]
    public int TotalPages { get; set; }

    [JsonPropertyName("hasNext")]
    public bool HasNext { get; set; }

    [JsonPropertyName("hasPrevious")]
    public bool HasPrevious { get; set; }
}

/// <summary>
/// Standard success response envelope. Every successful API response is
/// wrapped in this shape so the client always sees the same top-level
/// structure.
/// </summary>
/// <typeparam name="TData">Type of the response payload.</typeparam>
public class ApiResponse<TData>
{
    /// <summary>Always <c>true</c> for success responses.</summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>Short human-readable status message (e.g. <c>Operation successful</c>).</summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = "Operation successful";

    /// <summary>Response payload. For paginated responses this is the items list directly.</summary>
    [JsonPropertyName("data")]
    public TData? Data { get; set; }

    /// <summary>Field-level errors, or <c>null</c> for success responses.</summary>
    [JsonPropertyName("errors")]
    public object? Errors { get; set; }

    /// <summary>HTTP status code accompanying the response.</summary>
    [JsonIgnore]
    public int Status { get; set; } = 200;

    /// <summary>Build a 200-style success envelope.</summary>
    public static ApiResponse<TData> Ok(TData? data, string message = "Operation successful") => new()
    {
        Success = true, Message = message, Data = data, Status = 200
    };

    /// <summary>Build a 201-style created envelope.</summary>
    public static ApiResponse<TData> Created(TData? data, string message = "Created successfully") => new()
    {
        Success = true, Message = message, Data = data, Status = 201
    };
}

/// <summary>
/// Success response envelope for paginated list endpoints.
/// </summary>
/// <typeparam name="TData">Type of the items in the response payload.</typeparam>
public class PagedApiResponse<TData> : ApiResponse<IReadOnlyList<TData>>
{
    /// <summary>Pagination metadata. Only present on paginated list endpoints.</summary>
    [JsonPropertyName("meta")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public PaginationMeta? Meta { get; set; }
}


/// <summary>Error response envelope used for non-validation failures.</summary>
public class ApiErrorResponse
{
    /// <summary>Always <c>false</c> for error responses.</summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; } = false;

    /// <summary>Short human-readable error message.</summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = "An error occurred";

    /// <summary>Additional structured error context, if any.</summary>
    [JsonPropertyName("data")]
    public object? Data { get; set; }

    /// <summary>Field-level errors, if any.</summary>
    [JsonPropertyName("errors")]
    public object? Errors { get; set; }
}

/// <summary>
/// Non-generic success response envelope used by helpers that return
/// arbitrary payloads (e.g. <c>ApiResponse.Ok</c>).
/// </summary>
public class ApiResponse
{
    /// <summary><c>true</c> for success responses.</summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>Short human-readable status message.</summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = "Operation successful";

    /// <summary>Response payload, or <c>null</c>.</summary>
    [JsonPropertyName("data")]
    public object? Data { get; set; }

    /// <summary>Field-level errors, or <c>null</c>.</summary>
    [JsonPropertyName("errors")]
    public object? Errors { get; set; }

    /// <summary>Build a success envelope for an arbitrary payload.</summary>
    public static ApiResponse Ok(object? data = null, string message = "Operation successful") => new()
    {
        Success = true, Message = message, Data = data
    };

    /// <summary>Build a 201-style created envelope for an arbitrary payload.</summary>
    public static ApiResponse Created(object? data = null, string message = "Created successfully") => new()
    {
        Success = true, Message = message, Data = data
    };

    /// <summary>Build a failure envelope.</summary>
    public static ApiResponse Fail(string message, object? errors = null) => new()
    {
        Success = false, Message = message, Errors = errors
    };
}

/// <summary>Single field-level error entry returned with validation responses.</summary>
public class ApiErrorBody
{
    /// <summary>Name of the field that failed validation (or <c>null</c> for object-level errors).</summary>
    [JsonPropertyName("field")]
    public string? Field { get; set; }

    /// <summary>Human-readable error message.</summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

/// <summary>Validation failure response envelope. Returned with HTTP 422.</summary>
public class ApiValidationErrorResponse
{
    /// <summary>Always <c>false</c> for validation errors.</summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; } = false;

    /// <summary>Short human-readable error message.</summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = "Validation failed";

    /// <summary>Always <c>null</c> for validation errors.</summary>
    [JsonPropertyName("data")]
    public object? Data { get; set; }

    /// <summary>List of field-level errors.</summary>
    [JsonPropertyName("errors")]
    public List<ApiErrorBody>? Errors { get; set; }
}
