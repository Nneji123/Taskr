using System.Text.Json.Serialization;

namespace API.Common;

public class ApiResponse<TData>
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = "Operation successful";

    [JsonPropertyName("data")]
    public TData? Data { get; set; }

    [JsonPropertyName("errors")]
    public object? Errors { get; set; }

    [JsonIgnore]
    public int Status { get; set; } = 200;

    public static ApiResponse<TData> Ok(TData? data, string message = "Operation successful") => new()
    {
        Success = true, Message = message, Data = data, Status = 200
    };

    public static ApiResponse<TData> Created(TData? data, string message = "Created successfully") => new()
    {
        Success = true, Message = message, Data = data, Status = 201
    };
}

public class ApiErrorResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; } = false;

    [JsonPropertyName("message")]
    public string Message { get; set; } = "An error occurred";

    [JsonPropertyName("data")]
    public object? Data { get; set; }

    [JsonPropertyName("errors")]
    public object? Errors { get; set; }
}

public class ApiResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = "Operation successful";

    [JsonPropertyName("data")]
    public object? Data { get; set; }

    [JsonPropertyName("errors")]
    public object? Errors { get; set; }

    public static ApiResponse Ok(object? data = null, string message = "Operation successful") => new()
    {
        Success = true, Message = message, Data = data
    };

    public static ApiResponse Created(object? data = null, string message = "Created successfully") => new()
    {
        Success = true, Message = message, Data = data
    };

    public static ApiResponse Fail(string message, object? errors = null) => new()
    {
        Success = false, Message = message, Errors = errors
    };
}

public class ApiErrorBody
{
    [JsonPropertyName("field")]
    public string? Field { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

public class ApiValidationErrorResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; } = false;

    [JsonPropertyName("message")]
    public string Message { get; set; } = "Validation failed";

    [JsonPropertyName("data")]
    public object? Data { get; set; }

    [JsonPropertyName("errors")]
    public List<ApiErrorBody>? Errors { get; set; }
}
