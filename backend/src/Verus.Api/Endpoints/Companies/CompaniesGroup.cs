using FastEndpoints;

namespace Verus.Api.Endpoints.Companies;

/// <summary>Route group: <c>/api/v{version}/companies</c>.</summary>
internal sealed class CompaniesGroup : Group
{
    public const string Route = "companies";

    public CompaniesGroup() => Configure(Route, endpoint => endpoint.Description(d => d.WithTags("Companies")));
}
