using System.Text.Json.Serialization;
using FluentValidation;

namespace API.Common;

/// <summary>
/// Shared pagination, sorting, and filtering parameters accepted by all list
/// endpoints. Concrete list queries inherit from this class and add their
/// own feature-specific filters.
/// </summary>
public class PagedRequest
{
    /// <summary>1-indexed page number. Defaults to <c>1</c>.</summary>
    [JsonPropertyName("page")]
    public int Page { get; set; } = 1;

    /// <summary>Number of items per page. Defaults to <c>20</c>, max <c>100</c>.</summary>
    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; } = 20;

    /// <summary>Sort expression. Prefix a field name with <c>-</c> for descending order (e.g. <c>-createdAt</c>).</summary>
    [JsonPropertyName("sort")]
    public string? Sort { get; set; }

    /// <summary>Optional full-text search applied to the primary name/title field of the resource.</summary>
    [JsonPropertyName("search")]
    public string? Search { get; set; }

    /// <summary>Filter results after this date (inclusive). ISO 8601 format.</summary>
    [JsonPropertyName("startDate")]
    public DateTime? StartDate { get; set; }

    /// <summary>Filter results before this date (inclusive). ISO 8601 format.</summary>
    [JsonPropertyName("endDate")]
    public DateTime? EndDate { get; set; }
}

/// <summary>Validation rules for <see cref="PagedRequest"/> shared by all list endpoints.</summary>
public class PagedRequestValidator : AbstractValidator<PagedRequest>
{
    public PagedRequestValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
