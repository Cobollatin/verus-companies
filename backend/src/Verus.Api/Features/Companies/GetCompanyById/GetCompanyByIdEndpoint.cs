using FastEndpoints;
using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using Verus.Api.Common;
using Verus.Application.Features.Companies;
using Verus.Application.Features.Companies.GetCompanyById;
using ProblemDetails = FastEndpoints.ProblemDetails;

namespace Verus.Api.Features.Companies.GetCompanyById;

internal sealed class GetCompanyByIdEndpoint(IMediator mediator)
    : Endpoint<GetCompanyByIdRequest, Results<Ok<CompanyResponse>, ApiProblem>>
{
    public override void Configure()
    {
        // No route constraint on purpose: a malformed id is a 400 (bad input), an unknown id a 404.
        Get("{id}");
        Group<CompaniesGroup>();
        Version(1);
        AllowAnonymous();
        Description(d => d.Produces<ProblemDetails>(StatusCodes.Status404NotFound, ApiProblem.ContentType));
    }

    public override async Task<Results<Ok<CompanyResponse>, ApiProblem>> ExecuteAsync(
        GetCompanyByIdRequest req,
        CancellationToken ct
    )
    {
        var result = await mediator.Send(new GetCompanyByIdQuery(req.Id), ct);

        return result.ToOk(HttpContext);
    }
}
