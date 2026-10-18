namespace Verus.Application.Features.Companies;

/// <summary>Public representation of a company.</summary>
/// <param name="Id">Unique identifier.</param>
/// <param name="Name">Company name.</param>
/// <param name="WebsiteUrl">Normalized website URL.</param>
/// <param name="Domain">Registrable domain of the website, e.g. <c>example.co.uk</c>.</param>
/// <param name="CreatedAt">When the company was registered (UTC).</param>
/// <param name="RelevanceScore">Search relevance in [0, 1]; only present on relevance-ranked searches.</param>
public sealed record CompanyResponse(
    Guid Id,
    string Name,
    string WebsiteUrl,
    string Domain,
    DateTimeOffset CreatedAt,
    double? RelevanceScore = null
);
