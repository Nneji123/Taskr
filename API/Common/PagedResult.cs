using System.Text.Json.Serialization;

namespace API.Common;

public class PagedResult<T>
{
    [JsonPropertyName("items")]
    public IReadOnlyList<T> Items { get; set; } = [];

    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; }

    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }

    [JsonPropertyName("totalPages")]
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    [JsonPropertyName("hasNext")]
    public bool HasNext => Page < TotalPages;

    [JsonPropertyName("hasPrevious")]
    public bool HasPrevious => Page > 1;
}
