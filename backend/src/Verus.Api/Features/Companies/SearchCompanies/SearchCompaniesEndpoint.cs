using FastEndpoints;
using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using Verus.Api.Common;
using Verus.Application.Features.Companies;
using Verus.Application.Features.Companies.SearchCompanies;

namespace Verus.Api.Features.Companies.SearchCompanies;

internal sealed class SearchCompaniesEndpoint(IMediator mediator)
    : Endpoint<SearchCompaniesRequest, Results<Ok<PagedResponse<CompanyResponse>>, ApiProblem>>
{
    public override void Configure()
    {
        Get("");
        Group<CompaniesGroup>();
        Version(1);
        AllowAnonymous();
    }

    public override async Task<Results<Ok<PagedResponse<CompanyResponse>>, ApiProblem>> ExecuteAsync(
        SearchCompaniesRequest req,
        CancellationToken ct
    )
    {
        var query = new SearchCompaniesQuery(req.Name, req.Domain, req.Q, req.Sort, req.Page, req.PageSize);
        var result = await mediator.Send(query, ct);

        return result.Then(PagedResponse.From).ToOk(HttpContext);
    }
}
