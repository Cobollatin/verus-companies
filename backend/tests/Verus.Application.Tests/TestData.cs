using Microsoft.Extensions.Time.Testing;
using Verus.Domain.Companies;
using Verus.Domain.Relevance;

namespace Verus.Application.Tests;

internal static class TestData
{
    public static readonly DateTimeOffset Now = new(2026, 10, 16, 12, 0, 0, TimeSpan.Zero);

    public static CompanyName Name(string value) => CompanyName.Create(value).Value;

    public static WebsiteUrl Website(string value) => WebsiteUrl.Create(value).Value;

    public static Company Company(string name, string website, DateTimeOffset? createdAt = null) =>
        Domain
            .Companies.Company.Create(
                Name(name),
                Website(website),
                new RelevanceAssessment(true, 1, RelevanceMethod.Domain, "Test."),
                new FakeTimeProvider(createdAt ?? Now)
            )
            .Value;
}
