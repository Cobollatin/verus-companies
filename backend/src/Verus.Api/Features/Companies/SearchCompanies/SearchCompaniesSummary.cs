using FastEndpoints;

namespace Verus.Api.Features.Companies.SearchCompanies;

internal sealed class SearchCompaniesSummary : Summary<SearchCompaniesEndpoint>
{
    public SearchCompaniesSummary()
    {
        Summary = "List, filter and search companies";
        Description =
            "Without parameters returns every company (paged). Filter by `name` and/or `domain`; "
            + "use `q` for a relevance-ranked search. An empty result is a 200 with no items.";
        Responses[StatusCodes.Status200OK] = "A page of companies (possibly empty).";
        Responses[StatusCodes.Status400BadRequest] = "Invalid query parameters.";
    }
}
