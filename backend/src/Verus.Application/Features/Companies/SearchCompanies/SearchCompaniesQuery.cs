using ErrorOr;
using Mediator;
using Verus.Application.Common.Paging;

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
