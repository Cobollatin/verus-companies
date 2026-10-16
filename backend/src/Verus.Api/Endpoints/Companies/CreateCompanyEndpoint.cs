using FastEndpoints;
using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using Verus.Api.Common;
using Verus.Application.Features.Companies;
using Verus.Application.Features.Companies.CreateCompany;
using ProblemDetails = FastEndpoints.ProblemDetails;

namespace Verus.Api.Endpoints.Companies;

/// <summary>Request body for registering a company.</summary>
/// <param name="Name">Company name, e.g. "Microsoft".</param>
/// <param name="WebsiteUrl">Absolute http(s) URL of the company website, e.g. "https://www.microsoft.com".</param>
public sealed record CreateCompanyRequest(string? Name, string? WebsiteUrl);

internal sealed class CreateCompanyEndpoint(IMediator mediator)
    : Endpoint<CreateCompanyRequest, Results<Created<CompanyResponse>, ApiProblem>>
{
    public override void Configure()
    {
        Post("");
        Group<CompaniesGroup>();
        Version(1);
        AllowAnonymous();
        Description(d => d.Produces<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json"));
        Summary(s =>
        {
            s.Summary = "Register a company";
            s.Description =
                "Validates the name and URL, checks that the name is relevant to the website "
                + "(domain matching, then home-page metadata), and stores the company.";
            s.ExampleRequest = new CreateCompanyRequest("Microsoft", "https://www.microsoft.com");
            s.Responses[StatusCodes.Status201Created] = "The company was created.";
            s.Responses[StatusCodes.Status400BadRequest] = "Invalid input or the name is not relevant to the website.";
            s.Responses[StatusCodes.Status409Conflict] = "A company with the same website domain already exists.";
        });
    }

    public override async Task<Results<Created<CompanyResponse>, ApiProblem>> ExecuteAsync(
        CreateCompanyRequest req,
        CancellationToken ct
    )
    {
        var result = await mediator.Send(new CreateCompanyCommand(req.Name, req.WebsiteUrl), ct);

        return result.Match<Results<Created<CompanyResponse>, ApiProblem>>(
            company => TypedResults.Created($"{HttpContext.Request.Path.Value?.TrimEnd('/')}/{company.Id}", company),
            errors => errors.ToProblem(HttpContext)
        );
    }
}
