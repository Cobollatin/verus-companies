using Microsoft.Extensions.Time.Testing;
using Verus.Domain.Companies;
using Verus.Domain.Relevance;

namespace Verus.Domain.Tests;

internal static class TestData
{
    public static CompanyName Name(string value) => CompanyName.Create(value).Value;

    public static WebsiteUrl Website(string value) => WebsiteUrl.Create(value).Value;

    public static Company Company(string name, string website) =>
        Domain
            .Companies.Company.Create(
                Name(name),
                Website(website),
                new RelevanceAssessment(true, 1, RelevanceMethod.Domain, "Test."),
                new FakeTimeProvider()
            )
            .Value;
}
