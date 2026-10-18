using FastEndpoints;

namespace Verus.Api.Features.Companies.CreateCompany;

internal sealed class CreateCompanySummary : Summary<CreateCompanyEndpoint>
{
    public CreateCompanySummary()
    {
        Summary = "Register a company";
        Description =
            "Validates the name and URL, checks that the name is relevant to the website "
            + "(domain matching, then home-page metadata), and stores the company.";
        ExampleRequest = new CreateCompanyRequest("Microsoft", "https://www.microsoft.com");
        Responses[StatusCodes.Status201Created] = "The company was created.";
        Responses[StatusCodes.Status400BadRequest] = "Invalid input or the name is not relevant to the website.";
        Responses[StatusCodes.Status409Conflict] = "A company with the same website domain already exists.";
    }
}
