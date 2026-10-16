using Verus.Domain.Companies;

namespace Verus.Domain.Tests.Companies;

public sealed class CompanySearchScorerTests
{
    private static readonly Company Microsoft = TestData.Company("Microsoft Corporation", "https://www.microsoft.com");

    [Theory]
    [InlineData("microsoft corporation", 1.0)]
    [InlineData("Microsoft", 1.0)]
    [InlineData("microsoft.com", 0.95)]
    [InlineData("micro", 0.85)]
    [InlineData("corp", 0.7)]
    [InlineData("soft", 0.6)]
    public void Score_RanksByMatchStrength(string query, double expected) =>
        CompanySearchScorer.Score(Microsoft, query).ShouldBe(expected, tolerance: 0.001);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("banana")]
    [InlineData("!!!")]
    public void Score_WithUnrelatedOrEmptyQuery_IsZero(string query) =>
        CompanySearchScorer.Score(Microsoft, query).ShouldBe(0);

    [Fact]
    public void Score_WithTypo_IsLowButPositive()
    {
        var score = CompanySearchScorer.Score(Microsoft, "microsft");

        score.ShouldBeGreaterThan(0);
        score.ShouldBeLessThan(0.55);
    }

    [Fact]
    public void Score_OrdersCompaniesByRelevance()
    {
        var companies = new[]
        {
            TestData.Company("Soft Kitty Toys", "https://softkitty.com"),
            TestData.Company("Microsoft", "https://microsoft.com"),
            TestData.Company("Micron Technology", "https://www.micron.com"),
        };

        var ranked = companies
            .Select(c => (Company: c, Score: CompanySearchScorer.Score(c, "microsoft")))
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .Select(x => x.Company.Name.Value)
            .ToList();

        ranked[0].ShouldBe("Microsoft");
        ranked.ShouldNotContain("Soft Kitty Toys");
    }
}
