using Verus.Domain.Companies;

namespace Verus.Domain.Relevance;

/// <summary>
/// Fallback strategy for brands whose domain differs from their name (e.g. "Alphabet" ↔ abc.xyz):
/// does the site's own metadata name the company?
/// </summary>
public static class ContentMatcher
{
    private const int MinContainmentLength = 3;
    private static readonly char[] TitleSeparators = ['|', '-', '–', '—', ':', '·', '•'];

    public static RelevanceAssessment Assess(CompanyName name, WebsiteMetadata metadata, RelevancePolicy policy)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(metadata);

        var normalizedName = NormalizedText.FromCompanyName(name.Value);

        var sources = new (string Label, string? Text)[]
        {
            ("site name", metadata.SiteName),
            ("application name", metadata.ApplicationName),
            ("page title", metadata.Title),
        };

        var best =
            sources
                .Where(s => !string.IsNullOrWhiteSpace(s.Text))
                .SelectMany(s => Candidates(normalizedName, s.Label, s.Text!.Trim()))
                .MaxBy(c => c.Score)
            ?? RelevanceCandidate.None;

        if (best.Score < policy.Threshold)
        {
            best = best with { Reason = $"The website's title and metadata do not mention '{name.Value}'." };
        }

        return RelevanceAssessment.From(best, RelevanceMethod.Content, policy);
    }

    private static IEnumerable<RelevanceCandidate> Candidates(NormalizedText name, string source, string text)
    {
        if (name.IsEmpty)
        {
            yield break;
        }

        var content = NormalizedText.FromText(text);

        if (
            name.Squashed.Length >= MinContainmentLength
            && content.Squashed.Contains(name.Squashed, StringComparison.Ordinal)
        )
        {
            yield return new(0.9, $"The website {source} '{text}' mentions the company name.");
        }

        // Titles are usually "Brand | Tagline": compare each segment on its own.
        foreach (var segment in text.Split(TitleSeparators, StringSplitOptions.RemoveEmptyEntries))
        {
            var normalizedSegment = NormalizedText.FromCompanyName(segment);
            var similarity = Math.Min(TextSimilarity.Of(name.Squashed, normalizedSegment.Squashed), 0.95);
            yield return new(
                similarity,
                $"The website {source} '{text}' is {similarity:P0} similar to the company name."
            );
        }
    }
}
