namespace Verus.Api.Features.Companies.CreateCompany;

/// <summary>Request body for registering a company.</summary>
/// <param name="Name">Company name, e.g. "Microsoft".</param>
/// <param name="WebsiteUrl">Absolute http(s) URL of the company website, e.g. "https://www.microsoft.com".</param>
public sealed record CreateCompanyRequest(string? Name, string? WebsiteUrl);
