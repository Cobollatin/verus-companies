using FastEndpoints;
using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using Verus.Api.Common;
using Verus.Application.Features.Companies;
using Verus.Application.Features.Companies.CreateCompany;
using ProblemDetails = FastEndpoints.ProblemDetails;

namespace Verus.Api.Features.Companies.CreateCompany;

internal sealed class CreateCompanyEndpoint(IMediator mediator)
    : Endpoint<CreateCompanyRequest, Results<Created<CompanyResponse>, ApiProblem>>
{
    public override void Configure()
    {
        Post("");
        Group<CompaniesGroup>();
        Version(1);
        AllowAnonymous();
        Description(d => d.Produces<ProblemDetails>(StatusCodes.Status409Conflict, ApiProblem.ContentType));
    }

    public override async Task<Results<Created<CompanyResponse>, ApiProblem>> ExecuteAsync(
        CreateCompanyRequest req,
        CancellationToken ct
    )
    {
        var result = await mediator.Send(new CreateCompanyCommand(req.Name, req.WebsiteUrl), ct);

        return result.ToCreated(HttpContext, company => $"{HttpContext.Request.Path.Value?.TrimEnd('/')}/{company.Id}");
    }
}
