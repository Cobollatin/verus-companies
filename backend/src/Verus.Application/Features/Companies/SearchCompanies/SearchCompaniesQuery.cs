using ErrorOr;
using FluentValidation;
using Mediator;
using Verus.Application.Abstractions;
using Verus.Application.Common.Paging;
using Verus.Domain.Companies;

namespace Verus.Application.Features.Companies.SearchCompanies;

/// <summary>List, filter and relevance-search companies.</summary>
/// <param name="Name">Partial or full company name (case-insensitive).</param>
/// <param name="Domain">Website domain, e.g. <c>example.com</c> (also matches subdomains) or a fragment such as <c>example</c>.</param>
/// <param name="Q">Free-text relevance search over name and domain; results are ranked by relevance.</param>
/// <param name="Sort"><c>relevance</c> (default when <paramref name="Q"/> is set), <c>name</c>, <c>-name</c>, <c>createdAt</c>, <c>-createdAt</c>.</param>
/// <param name="Page">1-based page number.</param>
/// <param name="PageSize">Items per page (1–100).</param>
public sealed record SearchCompaniesQuery(
    string? Name = null,
    string? Domain = null,
    string? Q = null,
    string? Sort = null,
    int Page = PageRequest.DefaultPage,
    int PageSize = PageRequest.DefaultPageSize
) : IQuery<ErrorOr<PagedResult<CompanyResponse>>>;

public sealed class SearchCompaniesValidator : AbstractValidator<SearchCompaniesQuery>
{
    public const int MaxTermLength = 200;

    public SearchCompaniesValidator()
    {
        RuleFor(q => q.Name)
            .MaximumLength(MaxTermLength)
            .WithErrorCode("Search.Name.TooLong")
            .WithMessage($"Name filter must be at most {MaxTermLength} characters.");
        RuleFor(q => q.Q)
            .MaximumLength(MaxTermLength)
            .WithErrorCode("Search.Q.TooLong")
            .WithMessage($"Search query must be at most {MaxTermLength} characters.");
        RuleFor(q => q.Domain)
            .Must(d => d!.Length <= 253 && DomainQuery.Normalize(d) is not null)
            .When(q => !string.IsNullOrWhiteSpace(q.Domain))
            .WithErrorCode("Search.Domain.Invalid")
            .WithMessage("Domain must look like 'example.com' or 'example'.");
        RuleFor(q => q.Sort)
            .Must(s => SortOrder.TryParse(s, out _))
            .WithErrorCode("Search.Sort.Invalid")
            .WithMessage($"Sort must be one of: {string.Join(", ", SortOrder.Allowed)}.");
        RuleFor(q => q.Sort)
            .Must(s => !SortOrder.IsRelevance(s))
            .When(q => string.IsNullOrWhiteSpace(q.Q))
            .WithErrorCode("Search.Sort.RelevanceRequiresQuery")
            .WithMessage("Sort 'relevance' requires a search query 'q'.");
        RuleFor(q => q.Page)
            .GreaterThanOrEqualTo(1)
            .WithErrorCode("Search.Page.Invalid")
            .WithMessage("Page must be 1 or greater.");
        RuleFor(q => q.PageSize)
            .InclusiveBetween(1, PageRequest.MaxPageSize)
            .WithErrorCode("Search.PageSize.Invalid")
            .WithMessage($"Page size must be between 1 and {PageRequest.MaxPageSize}.");
    }
}

public sealed class SearchCompaniesHandler(ICompanyRepository repository)
    : IQueryHandler<SearchCompaniesQuery, ErrorOr<PagedResult<CompanyResponse>>>
{
    public async ValueTask<ErrorOr<PagedResult<CompanyResponse>>> Handle(
        SearchCompaniesQuery query,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var filter = new CompanyFilter(
            string.IsNullOrWhiteSpace(query.Name) ? null : query.Name.Trim(),
            DomainQuery.Normalize(query.Domain)
        );
        var page = new PageRequest(query.Page, query.PageSize);
        var sort = SortOrder.Parse(query.Sort);

        if (string.IsNullOrWhiteSpace(query.Q))
        {
            var result = await repository.ListAsync(filter, sort ?? CompanySort.Name, page, cancellationToken);
            return result.Map(c => c.ToResponse());
        }

        // Relevance ranking runs in memory over the filtered candidates; a database implementation
        // would push this down to full-text search behind the same port.
        var candidates = await repository.ListAllAsync(filter, cancellationToken);
        var hits = candidates
            .Select(c => (Company: c, Score: CompanySearchScorer.Score(c, query.Q)))
            .Where(hit => hit.Score > 0);

        var ordered = sort switch
        {
            null => hits.OrderByDescending(h => h.Score)
                .ThenBy(h => h.Company.Name.Value, StringComparer.OrdinalIgnoreCase),
            CompanySort.Name => hits.OrderBy(h => h.Company.Name.Value, StringComparer.OrdinalIgnoreCase),
            CompanySort.NameDescending => hits.OrderByDescending(
                h => h.Company.Name.Value,
                StringComparer.OrdinalIgnoreCase
            ),
            CompanySort.CreatedAt => hits.OrderBy(h => h.Company.CreatedAt),
            CompanySort.CreatedAtDescending => hits.OrderByDescending(h => h.Company.CreatedAt),
            _ => throw new InvalidOperationException($"Unsupported sort {sort}."),
        };

        return PagedResult.From([.. ordered], page).Map(h => h.Company.ToResponse(Math.Round(h.Score, 4)));
    }
}

/// <summary>Parses the <c>sort</c> query parameter. <c>null</c> sort means "by relevance".</summary>
internal static class SortOrder
{
    public const string Relevance = "relevance";

    private static readonly Dictionary<string, CompanySort?> Map = new(StringComparer.OrdinalIgnoreCase)
    {
        [Relevance] = null,
        ["name"] = CompanySort.Name,
        ["-name"] = CompanySort.NameDescending,
        ["createdAt"] = CompanySort.CreatedAt,
        ["-createdAt"] = CompanySort.CreatedAtDescending,
    };

    public static IEnumerable<string> Allowed => Map.Keys;

    public static bool IsRelevance(string? value) =>
        string.Equals(value?.Trim(), Relevance, StringComparison.OrdinalIgnoreCase);

    /// <summary>Parses a value already accepted by <see cref="TryParse"/>.</summary>
    public static CompanySort? Parse(string? value) => string.IsNullOrWhiteSpace(value) ? null : Map[value.Trim()];

    public static bool TryParse(string? value, out CompanySort? sort)
    {
        sort = null;
        return string.IsNullOrWhiteSpace(value) || Map.TryGetValue(value.Trim(), out sort);
    }
}

/// <summary>Accepts <c>example.com</c>, <c>www.example.com</c> or a pasted URL and reduces it to a host fragment.</summary>
internal static class DomainQuery
{
    public static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var domain = value.Trim().ToLowerInvariant();

        var schemeEnd = domain.IndexOf("://", StringComparison.Ordinal);
        if (schemeEnd >= 0)
        {
            domain = domain[(schemeEnd + 3)..];
        }

        var pathStart = domain.IndexOfAny(['/', '?', '#', ':']);
        if (pathStart >= 0)
        {
            domain = domain[..pathStart];
        }

        if (domain.StartsWith("www.", StringComparison.Ordinal))
        {
            domain = domain[4..];
        }

        domain = domain.Trim('.');

        return domain.Length > 0 && domain.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '.') ? domain : null;
    }
}
