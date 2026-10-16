using System.Net.Http.Json;

namespace Verus.Api.IntegrationTests.Infrastructure;

// Client-side view of the HTTP contract, deliberately independent from the server's types.

public sealed record CompanyDto(
    Guid Id,
    string Name,
    string WebsiteUrl,
    string Domain,
    DateTimeOffset CreatedAt,
    double? RelevanceScore
);

public sealed record PageDto(
    IReadOnlyList<CompanyDto> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages,
    bool HasNextPage,
    bool HasPreviousPage
);

public sealed record ProblemErrorDto(string Name, string Reason, string? Code);

public sealed record ProblemDto(
    int Status,
    string Title,
    string Detail,
    string Instance,
    IReadOnlyList<ProblemErrorDto> Errors
);

internal static class HttpExtensions
{
    public const string CompaniesRoute = "/api/v1/companies";

    public static Task<HttpResponseMessage> CreateCompanyAsync(
        this HttpClient client,
        string? name,
        string? websiteUrl
    ) => client.PostAsJsonAsync(CompaniesRoute, new { name, websiteUrl }, TestContext.Current.CancellationToken);

    public static async Task<T> ReadAsync<T>(this HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<T>(TestContext.Current.CancellationToken))!;
}
