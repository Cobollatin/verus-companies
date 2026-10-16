using Verus.Domain.Relevance;

namespace Verus.Domain.Companies;

/// <summary>
/// Ranks a company against a free-text search query. Higher is better; 0 means "not a match".
/// Precedence: exact name → exact domain → name prefix → domain prefix → word prefixes → substring → fuzzy.
/// </summary>
public static class CompanySearchScorer
{
    private const double FuzzyThreshold = 0.85;

    public static double Score(Company company, string query)
    {
        ArgumentNullException.ThrowIfNull(company);

        if (string.IsNullOrWhiteSpace(query))
        {
            return 0;
        }

        var q = NormalizedText.FromText(query);
        if (q.IsEmpty)
        {
            return 0;
        }

        var name = NormalizedText.FromCompanyName(company.Name.Value);
        var fullName = NormalizedText.FromText(company.Name.Value);
        var label = NormalizedText.FromText(company.Website.DomainLabel).Squashed;
        var host = company.Website.Host;
        var rawQuery = query.Trim().ToLowerInvariant();

        if (q.Squashed == name.Squashed || q.Squashed == fullName.Squashed)
        {
            return 1.0;
        }

        if (q.Squashed == label || rawQuery == company.Website.RegistrableDomain)
        {
            return 0.95;
        }

        if (fullName.Squashed.StartsWith(q.Squashed, StringComparison.Ordinal))
        {
            return 0.85;
        }

        if (label.StartsWith(q.Squashed, StringComparison.Ordinal))
        {
            return 0.8;
        }

        if (q.Tokens.All(qt => fullName.Tokens.Any(t => t.StartsWith(qt, StringComparison.Ordinal))))
        {
            return 0.7;
        }

        if (fullName.Squashed.Contains(q.Squashed, StringComparison.Ordinal))
        {
            return 0.6;
        }

        if (label.Contains(q.Squashed, StringComparison.Ordinal) || host.Contains(rawQuery, StringComparison.Ordinal))
        {
            return 0.55;
        }

        var similarity = Math.Max(TextSimilarity.Of(q.Squashed, name.Squashed), TextSimilarity.Of(q.Squashed, label));
        return similarity >= FuzzyThreshold ? Math.Round(similarity * 0.5, 4) : 0;
    }
}
