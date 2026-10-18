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
        ArgumentNullException.ThrowIfNull(query);

        var q = NormalizedText.FromText(query);
        if (q.IsEmpty)
        {
            return 0;
        }

        var name = NormalizedText.FromCompanyName(company.Name.Value);
        var fullName = NormalizedText.FromText(company.Name.Value);
        var label = NormalizedText.FromText(company.Website.DomainLabel).Squashed;
        var rawQuery = query.Trim().ToLowerInvariant();

        return q.Squashed switch
        {
            var s when s == name.Squashed || s == fullName.Squashed => 1.0,
            var s when s == label || rawQuery == company.Website.RegistrableDomain => 0.95,
            var s when fullName.Squashed.StartsWith(s, StringComparison.Ordinal) => 0.85,
            var s when label.StartsWith(s, StringComparison.Ordinal) => 0.8,
            _ when q.Tokens.All(qt => fullName.Tokens.Any(t => t.StartsWith(qt, StringComparison.Ordinal))) => 0.7,
            var s when fullName.Squashed.Contains(s, StringComparison.Ordinal) => 0.6,
            var s
                when label.Contains(s, StringComparison.Ordinal)
                    || company.Website.Host.Contains(rawQuery, StringComparison.Ordinal) => 0.55,
            var s => FuzzyScore(s, name.Squashed, label),
        };
    }

    private static double FuzzyScore(string query, string name, string label)
    {
        var similarity = Math.Max(TextSimilarity.Of(query, name), TextSimilarity.Of(query, label));
        return similarity >= FuzzyThreshold ? Math.Round(similarity * 0.5, 4) : 0;
    }
}
