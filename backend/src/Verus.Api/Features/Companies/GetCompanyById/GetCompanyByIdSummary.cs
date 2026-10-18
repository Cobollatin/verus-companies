using FastEndpoints;

namespace Verus.Api.Features.Companies.GetCompanyById;

internal sealed class GetCompanyByIdSummary : Summary<GetCompanyByIdEndpoint>
{
    public GetCompanyByIdSummary()
    {
        Summary = "Get a company by id";
        Responses[StatusCodes.Status200OK] = "The company.";
        Responses[StatusCodes.Status400BadRequest] = "The id is not a valid GUID.";
        Responses[StatusCodes.Status404NotFound] = "No company has this id.";
    }
}
