using Microsoft.Extensions.Time.Testing;
using Verus.Domain.Companies;
using Verus.Domain.Relevance;

namespace Verus.Domain.Tests.Companies;

public sealed class CompanyTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 16, 12, 0, 0, TimeSpan.Zero);
    private readonly FakeTimeProvider _clock = new(Now);

    [Fact]
    public void Create_WhenRelevant_CreatesCompany()
    {
        var relevance = new RelevanceAssessment(true, 1, RelevanceMethod.Domain, "Exact match.");

        var result = Company.Create(TestData.Name("Acme"), TestData.Website("https://acme.com"), relevance, _clock);

        result.IsError.ShouldBeFalse();
        result.Value.ShouldSatisfyAllConditions(
            c => c.Id.Value.ShouldNotBe(Guid.Empty),
            c => c.Name.Value.ShouldBe("Acme"),
            c => c.Website.RegistrableDomain.ShouldBe("acme.com"),
            c => c.CreatedAt.ShouldBe(Now)
        );
    }

    [Fact]
    public void Create_WhenNotRelevant_ReturnsNotRelevantError()
    {
        var relevance = new RelevanceAssessment(false, 0.2, RelevanceMethod.Domain, "No resemblance.");

        var result = Company.Create(TestData.Name("Apple"), TestData.Website("https://ford.com"), relevance, _clock);

        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Company.Website.NotRelevant");
        result.FirstError.Description.ShouldContain("No resemblance.");
    }

    [Fact]
    public void Create_AssignsUniqueIds()
    {
        var relevance = new RelevanceAssessment(true, 1, RelevanceMethod.Domain, "Exact match.");
        var name = TestData.Name("Acme");
        var website = TestData.Website("https://acme.com");

        var first = Company.Create(name, website, relevance, _clock).Value;
        var second = Company.Create(name, website, relevance, _clock).Value;

        first.Id.ShouldNotBe(second.Id);
    }
}
