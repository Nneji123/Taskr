using FluentValidation;

namespace API.Common;

public class PagedRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Sort { get; set; }
    public string? Search { get; set; }

    /// <summary>Filter results after this date (inclusive). ISO 8601 format.</summary>
    public DateTime? StartDate { get; set; }

    /// <summary>Filter results before this date (inclusive). ISO 8601 format.</summary>
    public DateTime? EndDate { get; set; }
}

public class PagedRequestValidator : AbstractValidator<PagedRequest>
{
    public PagedRequestValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
