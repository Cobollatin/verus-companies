using Verus.Domain.Companies;

namespace Verus.Domain.Relevance;

/// <summary>
/// Offline, deterministic strategy: does the company name resemble the website's registrable domain?
/// Each rule yields scored evidence; the strongest wins.
/// <list type="table">
/// <item><term>1.00</term><description>exact: "Acme Corp" ↔ acme.com, "Bank of America" ↔ bankofamerica.com</description></item>
/// <item><term>0.90</term><description>containment: "Acme" ↔ getacme.com, "Delta Air Lines" ↔ delta.com</description></item>
/// <item><term>0.85</term><description>all name words in domain, or acronym: "International Business Machines" ↔ ibm.com</description></item>
/// <item><term>0..1</term><description>Jaro-Winkler similarity of the squashed strings (typos, small variations)</description></item>
/// </list>
/// </summary>
public static class NameDomainMatcher
{
    private const int MinContainmentLength = 3;

    public static RelevanceAssessment Assess(CompanyName name, WebsiteUrl website, RelevancePolicy policy)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(website);

        var normalizedName = NormalizedText.FromCompanyName(name.Value);
        var label = NormalizedText.FromText(website.DomainLabel).Squashed;
        var domain = website.RegistrableDomain;

        var best = Candidates(normalizedName, label, domain).MaxBy(c => c.Score) ?? RelevanceCandidate.None;

        if (best.Score < policy.Threshold)
        {
            best = best with
            {
                Reason = $"Domain '{domain}' does not resemble '{name.Value}' (best match {best.Score:P0}).",
            };
        }

        return RelevanceAssessment.From(best, RelevanceMethod.Domain, policy);
    }

    private static IEnumerable<RelevanceCandidate> Candidates(NormalizedText name, string label, string domain)
    {
        if (name.IsEmpty || label.Length == 0)
        {
            yield break;
        }

        if (label == name.Squashed || label == name.SquashedSignificant)
        {
            yield return new(1.0, $"Domain '{domain}' matches the company name exactly.");
        }

        if (name.Squashed.Length >= MinContainmentLength && label.Contains(name.Squashed, StringComparison.Ordinal))
        {
            yield return new(0.9, $"Domain '{domain}' contains the company name.");
        }

        if (label.Length >= MinContainmentLength && name.Squashed.StartsWith(label, StringComparison.Ordinal))
        {
            yield return new(0.9, $"The company name starts with domain '{domain}'.");
        }

        var words = name.SignificantTokens.Where(t => t.Length >= MinContainmentLength).ToList();
        if (words.Count >= 2 && words.TrueForAll(w => label.Contains(w, StringComparison.Ordinal)))
        {
            yield return new(0.85, $"Domain '{domain}' contains every word of the company name.");
        }

        if (label.Length >= 2 && name.Acronyms.Contains(label))
        {
            yield return new(0.85, $"Domain '{domain}' is the acronym of the company name.");
        }

        if (IsPartialAcronym(name.SignificantTokens, label))
        {
            yield return new(0.85, $"Domain '{domain}' abbreviates the company name.");
        }

        var similarity = Math.Min(TextSimilarity.Of(name.Squashed, label), 0.95);
        yield return new(similarity, $"Domain '{domain}' is {similarity:P0} similar to the company name.");
    }

    /// <summary>Initials of the leading words followed by the remaining words: "New York Times" ↔ nytimes.</summary>
    private static bool IsPartialAcronym(IReadOnlyList<string> tokens, string label) =>
        Enumerable
            .Range(2, Math.Max(tokens.Count - 2, 0))
            .Select(split => string.Concat(tokens.Take(split).Select(t => t[0])) + string.Concat(tokens.Skip(split)))
            .Contains(label, StringComparer.Ordinal);
}
