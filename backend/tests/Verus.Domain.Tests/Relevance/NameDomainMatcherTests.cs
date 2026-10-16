using Verus.Domain.Relevance;

namespace Verus.Domain.Tests.Relevance;

public sealed class NameDomainMatcherTests
{
    [Theory]
    [InlineData("Microsoft", "https://www.microsoft.com", 1.0)]
    [InlineData("Acme Corp", "https://acme.com", 1.0)]
    [InlineData("Bank of America", "https://www.bankofamerica.com", 1.0)]
    [InlineData("Coca-Cola Company", "https://www.coca-cola.com", 1.0)]
    [InlineData("Nestlé S.A.", "https://www.nestle.com", 1.0)]
    [InlineData("The Home Depot", "https://www.homedepot.com", 1.0)]
    [InlineData("BBC", "https://www.bbc.co.uk", 1.0)]
    [InlineData("Acme", "https://getacme.io", 0.9)]
    [InlineData("Delta Air Lines", "https://www.delta.com", 0.9)]
    [InlineData("International Business Machines", "https://www.ibm.com", 0.85)]
    [InlineData("Procter & Gamble", "https://pg.com", 0.85)]
    public void Assess_WhenNameMatchesDomain_IsRelevant(string name, string website, double expectedScore)
    {
        var result = NameDomainMatcher.Assess(TestData.Name(name), TestData.Website(website), RelevancePolicy.Default);

        result.ShouldSatisfyAllConditions(
            r => r.IsRelevant.ShouldBeTrue(),
            r => r.Score.ShouldBe(expectedScore, tolerance: 0.001),
            r => r.Method.ShouldBe(RelevanceMethod.Domain),
            r => r.Reason.ShouldNotBeNullOrWhiteSpace()
        );
    }

    [Fact]
    public void Assess_WithSmallTypo_IsRelevantByFuzzyMatch()
    {
        var result = NameDomainMatcher.Assess(
            TestData.Name("Mikrosoft"),
            TestData.Website("https://microsoft.com"),
            RelevancePolicy.Default
        );

        result.IsRelevant.ShouldBeTrue();
        result.Reason.ShouldContain("similar");
    }

    [Theory]
    [InlineData("Apple", "https://www.microsoft.com")]
    [InlineData("Google", "https://www.amazon.com")]
    [InlineData("Tesla Inc", "https://ford.com")]
    [InlineData("Alphabet", "https://abc.xyz")]
    public void Assess_WhenNameDoesNotMatchDomain_IsNotRelevant(string name, string website)
    {
        var result = NameDomainMatcher.Assess(TestData.Name(name), TestData.Website(website), RelevancePolicy.Default);

        result.IsRelevant.ShouldBeFalse();
        result.Reason.ShouldContain("does not resemble");
    }

    [Fact]
    public void Assess_RespectsPolicyThreshold()
    {
        var strict = new RelevancePolicy(0.95);

        var result = NameDomainMatcher.Assess(TestData.Name("Acme"), TestData.Website("https://getacme.io"), strict);

        result.IsRelevant.ShouldBeFalse();
    }
}
