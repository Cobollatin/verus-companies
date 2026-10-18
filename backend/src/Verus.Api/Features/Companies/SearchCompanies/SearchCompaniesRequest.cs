using FastEndpoints;
using Verus.Application.Common.Paging;

namespace Verus.Api.Features.Companies.SearchCompanies;

/// <summary>Query string for listing and searching companies. Every parameter is optional.</summary>
public sealed class SearchCompaniesRequest
{
    /// <summary>Partial or full company name (case-insensitive).</summary>
    [QueryParam]
    public string? Name { get; init; }

    /// <summary>Website domain, e.g. "example.com" (includes subdomains) or a fragment such as "example".</summary>
    [QueryParam]
    public string? Domain { get; init; }

    /// <summary>Free-text relevance search over name and domain; results are ordered by relevance.</summary>
    [QueryParam]
    public string? Q { get; init; }

    /// <summary>relevance | name | -name | createdAt | -createdAt. Defaults to relevance with q, otherwise name.</summary>
    [QueryParam]
    public string? Sort { get; init; }

    /// <summary>1-based page number.</summary>
    [QueryParam]
    public int Page { get; init; } = PageRequest.DefaultPage;

    /// <summary>Items per page (1–100).</summary>
    [QueryParam]
    public int PageSize { get; init; } = PageRequest.DefaultPageSize;
}
