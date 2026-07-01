using System.Text.Json.Serialization;

namespace API.Common;

/// <summary>
/// Generic paginated list response. Returned by all <c>GET</c> list endpoints.
/// </summary>
/// <typeparam name="T">Type of each list item.</typeparam>
public class PagedResult<T>
{
    /// <summary>The items on the current page.</summary>
    [JsonPropertyName("items")]
    public IReadOnlyList<T> Items { get; set; } = [];

    /// <summary>Current page number (1-indexed).</summary>
    [JsonPropertyName("page")]
    public int Page { get; set; }

    /// <summary>Maximum number of items returned per page.</summary>
    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; }

    /// <summary>Total number of items matching the query across all pages.</summary>
    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }

    /// <summary>Total number of pages given the current <c>pageSize</c>.</summary>
    [JsonPropertyName("totalPages")]
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    /// <summary>Whether there is at least one more page after the current one.</summary>
    [JsonPropertyName("hasNext")]
    public bool HasNext => Page < TotalPages;

    /// <summary>Whether there is at least one page before the current one.</summary>
    [JsonPropertyName("hasPrevious")]
    public bool HasPrevious => Page > 1;
}
