using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Verus.Application.Abstractions;
using Verus.Domain.Companies;
using Verus.Domain.Relevance;

namespace Verus.Application.Common.Relevance;

/// <summary>Decides whether a company name belongs to a website.</summary>
public interface IRelevanceEvaluator
{
    Task<RelevanceAssessment> EvaluateAsync(CompanyName name, WebsiteUrl website, CancellationToken cancellationToken);
}

/// <summary>
/// Strategy chain: (1) offline domain matching, then (2) — only if (1) fails and it is enabled —
/// inspection of the home page's title / <c>og:site_name</c> / <c>application-name</c>.
/// </summary>
public sealed partial class RelevanceEvaluator(
    IWebsiteContentProvider contentProvider,
    IOptions<RelevanceOptions> options,
    ILogger<RelevanceEvaluator> logger
) : IRelevanceEvaluator
{
    private readonly RelevanceOptions _options = options.Value;
    private readonly RelevancePolicy _policy = new(options.Value.Threshold);

    public async Task<RelevanceAssessment> EvaluateAsync(
        CompanyName name,
        WebsiteUrl website,
        CancellationToken cancellationToken
    )
    {
        var byDomain = NameDomainMatcher.Assess(name, website, _policy);
        if (byDomain.IsRelevant || !_options.EnableContentInspection)
        {
            return byDomain;
        }

        var metadata = await contentProvider.GetMetadataAsync(website, cancellationToken);
        if (metadata is null)
        {
            LogContentUnavailable(logger, website.Host);
            return byDomain with { Reason = $"{byDomain.Reason} The website content could not be inspected." };
        }

        var byContent = ContentMatcher.Assess(name, metadata, _policy);
        LogContentInspected(logger, website.Host, byContent.IsRelevant, byContent.Score);

        return byContent.IsRelevant ? byContent : byDomain with { Reason = $"{byDomain.Reason} {byContent.Reason}" };
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Website content of {Host} could not be inspected")]
    private static partial void LogContentUnavailable(ILogger logger, string host);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Website content of {Host} inspected: relevant={IsRelevant} score={Score:F2}"
    )]
    private static partial void LogContentInspected(ILogger logger, string host, bool isRelevant, double score);
}
