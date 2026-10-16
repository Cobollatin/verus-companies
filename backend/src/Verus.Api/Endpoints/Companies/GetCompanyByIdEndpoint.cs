using FastEndpoints;
using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using Verus.Api.Common;
using Verus.Application.Features.Companies;
using Verus.Application.Features.Companies.GetCompanyById;
using ProblemDetails = FastEndpoints.ProblemDetails;

namespace Verus.Api.Endpoints.Companies;

/// <summary>Route parameters for fetching one company.</summary>
public sealed class GetCompanyByIdRequest
{
    /// <summary>Company identifier (GUID).</summary>
    public Guid Id { get; init; }
}

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
        Description(d => d.Produces<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json"));
        Summary(s =>
        {
            s.Summary = "Get a company by id";
            s.Responses[StatusCodes.Status200OK] = "The company.";
            s.Responses[StatusCodes.Status400BadRequest] = "The id is not a valid GUID.";
            s.Responses[StatusCodes.Status404NotFound] = "No company has this id.";
        });
    }

    public override async Task<Results<Ok<CompanyResponse>, ApiProblem>> ExecuteAsync(
        GetCompanyByIdRequest req,
        CancellationToken ct
    )
    {
        var result = await mediator.Send(new GetCompanyByIdQuery(req.Id), ct);

        return result.Match<Results<Ok<CompanyResponse>, ApiProblem>>(
            company => TypedResults.Ok(company),
            errors => errors.ToProblem(HttpContext)
        );
    }
}
