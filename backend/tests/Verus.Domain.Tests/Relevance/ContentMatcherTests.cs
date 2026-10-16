using Verus.Domain.Relevance;

namespace Verus.Domain.Tests.Relevance;

public sealed class ContentMatcherTests
{
    [Theory]
    [InlineData("Alphabet Inc.", "Alphabet", null)]
    [InlineData("Acme", "Welcome to Acme Corporation | Home", null)]
    [InlineData("Meta Platforms", "Meta | Social Technology Company", "Meta Platforms")]
    public void Assess_WhenMetadataNamesTheCompany_IsRelevant(string name, string title, string? siteName)
    {
        var result = ContentMatcher.Assess(
            TestData.Name(name),
            new WebsiteMetadata(title, siteName, ApplicationName: null),
            RelevancePolicy.Default
        );

        result.IsRelevant.ShouldBeTrue();
        result.Method.ShouldBe(RelevanceMethod.Content);
    }

    [Theory]
    [InlineData("Acme", "Example Domain")]
    [InlineData("Apple", "Microsoft – AI, Cloud, Productivity")]
    public void Assess_WhenMetadataDoesNotNameTheCompany_IsNotRelevant(string name, string title)
    {
        var result = ContentMatcher.Assess(
            TestData.Name(name),
            new WebsiteMetadata(title, SiteName: null, ApplicationName: null),
            RelevancePolicy.Default
        );

        result.IsRelevant.ShouldBeFalse();
        result.Reason.ShouldContain("do not mention");
    }

    [Fact]
    public void Assess_WithEmptyMetadata_IsNotRelevant() =>
        ContentMatcher
            .Assess(TestData.Name("Acme"), new WebsiteMetadata(null, null, null), RelevancePolicy.Default)
            .IsRelevant.ShouldBeFalse();
}
