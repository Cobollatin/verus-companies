using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Verus.Application.Abstractions;
using Verus.Application.Common.Relevance;
using Verus.Domain.Companies;
using Verus.Domain.Relevance;

namespace Verus.Application.Tests.Common;

public sealed class RelevanceEvaluatorTests
{
    private readonly IWebsiteContentProvider _content = Substitute.For<IWebsiteContentProvider>();

    [Fact]
    public async Task EvaluateAsync_WhenDomainMatches_DoesNotFetchContent()
    {
        var result = await Evaluator()
            .EvaluateAsync(TestData.Name("Acme"), TestData.Website("https://acme.com"), CancellationToken.None);

        result.IsRelevant.ShouldBeTrue();
        result.Method.ShouldBe(RelevanceMethod.Domain);
        await _content.DidNotReceive().GetMetadataAsync(Arg.Any<WebsiteUrl>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EvaluateAsync_WhenDomainFailsButContentNamesCompany_IsRelevantByContent()
    {
        _content
            .GetMetadataAsync(Arg.Any<WebsiteUrl>(), Arg.Any<CancellationToken>())
            .Returns(new WebsiteMetadata("Alphabet", null, null));

        var result = await Evaluator()
            .EvaluateAsync(TestData.Name("Alphabet Inc."), TestData.Website("https://abc.xyz"), CancellationToken.None);

        result.IsRelevant.ShouldBeTrue();
        result.Method.ShouldBe(RelevanceMethod.Content);
    }

    [Fact]
    public async Task EvaluateAsync_WhenContentUnavailable_IsNotRelevantWithExplanation()
    {
        _content.GetMetadataAsync(Arg.Any<WebsiteUrl>(), Arg.Any<CancellationToken>()).Returns((WebsiteMetadata?)null);

        var result = await Evaluator()
            .EvaluateAsync(
                TestData.Name("Apple"),
                TestData.Website("https://www.microsoft.com"),
                CancellationToken.None
            );

        result.IsRelevant.ShouldBeFalse();
        result.Reason.ShouldContain("could not be inspected");
    }

    [Fact]
    public async Task EvaluateAsync_WhenContentInspectionDisabled_UsesDomainOnly()
    {
        var result = await Evaluator(enableContent: false)
            .EvaluateAsync(
                TestData.Name("Apple"),
                TestData.Website("https://www.microsoft.com"),
                CancellationToken.None
            );

        result.IsRelevant.ShouldBeFalse();
        await _content.DidNotReceive().GetMetadataAsync(Arg.Any<WebsiteUrl>(), Arg.Any<CancellationToken>());
    }

    private RelevanceEvaluator Evaluator(bool enableContent = true) =>
        new(
            _content,
            Options.Create(new RelevanceOptions { EnableContentInspection = enableContent }),
            NullLogger<RelevanceEvaluator>.Instance
        );
}
