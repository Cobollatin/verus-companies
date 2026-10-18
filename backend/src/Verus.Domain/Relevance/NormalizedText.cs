using System.Collections.Frozen;
using System.Globalization;
using System.Text;

namespace Verus.Domain.Relevance;

/// <summary>
/// Text reduced to comparable form: lower-case ASCII-folded tokens, plus the tokens "squashed" together
/// (<c>"Coca-Cola"</c> → <c>cocacola</c>) and acronyms (<c>"International Business Machines"</c> → <c>ibm</c>).
/// </summary>
public sealed class NormalizedText
{
    private static readonly FrozenSet<string> LegalSuffixes = FrozenSet.Create(
        StringComparer.Ordinal,
        "inc",
        "incorporated",
        "llc",
        "llp",
        "lp",
        "ltd",
        "limited",
        "corp",
        "corporation",
        "co",
        "company",
        "plc",
        "gmbh",
        "ag",
        "sa",
        "sas",
        "srl",
        "spa",
        "bv",
        "nv",
        "oy",
        "ab",
        "pty",
        "pte",
        "kk",
        "holdings",
        "group"
    );

    private static readonly FrozenSet<string> Connectors = FrozenSet.Create(
        StringComparer.Ordinal,
        "the",
        "and",
        "of",
        "for",
        "a"
    );

    private NormalizedText(IReadOnlyList<string> tokens)
    {
        Tokens = tokens;
        Squashed = string.Concat(tokens);
        SignificantTokens = [.. tokens.Where(t => !Connectors.Contains(t))];
        SquashedSignificant = string.Concat(SignificantTokens);
        Acronyms = BuildAcronyms(tokens, SignificantTokens);
    }

    public IReadOnlyList<string> Tokens { get; }

    /// <summary>Tokens without connector words such as "and", "of", "the".</summary>
    public IReadOnlyList<string> SignificantTokens { get; }

    public string Squashed { get; }

    public string SquashedSignificant { get; }

    public IReadOnlySet<string> Acronyms { get; }

    public bool IsEmpty => Squashed.Length == 0;

    /// <summary>Normalizes a company name, dropping trailing legal suffixes such as "Inc" or "GmbH".</summary>
    public static NormalizedText FromCompanyName(string name)
    {
        // Dotted abbreviations ("S.A.", "J.P.", "Inc.") become single tokens.
        var tokens = Tokenize(name.Replace(".", string.Empty, StringComparison.Ordinal));

        while (tokens.Count > 1 && LegalSuffixes.Contains(tokens[^1]))
        {
            tokens.RemoveAt(tokens.Count - 1);
        }

        if (tokens.Count > 1 && tokens[0] == "the")
        {
            tokens.RemoveAt(0);
        }

        return new NormalizedText(tokens);
    }

    /// <summary>Normalizes arbitrary text (domain labels, page titles, search queries) without dropping words.</summary>
    public static NormalizedText FromText(string text) => new(Tokenize(text));

    private static List<string> Tokenize(string text)
    {
        var folded = RemoveDiacritics(text).Replace("&", " and ", StringComparison.Ordinal);
        var separated = folded.Select(c => char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : ' ');

        return [.. new string([.. separated]).Split(' ', StringSplitOptions.RemoveEmptyEntries)];
    }

    private static string RemoveDiacritics(string text)
    {
        var withoutMarks = text.Normalize(NormalizationForm.FormD)
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark);

        return string.Concat(withoutMarks).Normalize(NormalizationForm.FormC);
    }

    private static FrozenSet<string> BuildAcronyms(IReadOnlyList<string> tokens, IReadOnlyList<string> significant)
    {
        var acronyms = new HashSet<string>(StringComparer.Ordinal);

        if (tokens.Count >= 2)
        {
            acronyms.Add(string.Concat(tokens.Select(t => t[0])));
        }

        if (significant.Count >= 2)
        {
            acronyms.Add(string.Concat(significant.Select(t => t[0])));
        }

        return acronyms.ToFrozenSet(StringComparer.Ordinal);
    }
}
