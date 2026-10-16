using FastEndpoints;
using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using Verus.Api.Common;
using Verus.Application.Common.Paging;
using Verus.Application.Features.Companies;
using Verus.Application.Features.Companies.SearchCompanies;

namespace Verus.Api.Endpoints.Companies;

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

/// <summary>A page of companies.</summary>
public sealed record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages,
    bool HasNextPage,
    bool HasPreviousPage
);

internal sealed class SearchCompaniesEndpoint(IMediator mediator)
    : Endpoint<SearchCompaniesRequest, Results<Ok<PagedResponse<CompanyResponse>>, ApiProblem>>
{
    public override void Configure()
    {
        Get("");
        Group<CompaniesGroup>();
        Version(1);
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "List, filter and search companies";
            s.Description =
                "Without parameters returns every company (paged). Filter by `name` and/or `domain`; "
                + "use `q` for a relevance-ranked search. An empty result is a 200 with no items.";
            s.Responses[StatusCodes.Status200OK] = "A page of companies (possibly empty).";
            s.Responses[StatusCodes.Status400BadRequest] = "Invalid query parameters.";
        });
    }

    public override async Task<Results<Ok<PagedResponse<CompanyResponse>>, ApiProblem>> ExecuteAsync(
        SearchCompaniesRequest req,
        CancellationToken ct
    )
    {
        var result = await mediator.Send(
            new SearchCompaniesQuery(req.Name, req.Domain, req.Q, req.Sort, req.Page, req.PageSize),
            ct
        );

        return result.Match<Results<Ok<PagedResponse<CompanyResponse>>, ApiProblem>>(
            page =>
                TypedResults.Ok(
                    new PagedResponse<CompanyResponse>(
                        page.Items,
                        page.Page,
                        page.PageSize,
                        page.TotalCount,
                        page.TotalPages,
                        page.HasNextPage,
                        page.HasPreviousPage
                    )
                ),
            errors => errors.ToProblem(HttpContext)
        );
    }
}
