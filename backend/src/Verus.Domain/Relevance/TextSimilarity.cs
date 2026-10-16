using F23.StringSimilarity;

namespace Verus.Domain.Relevance;

/// <summary>Jaro-Winkler similarity: tolerant to typos and rewards shared prefixes, which suits brand names.</summary>
internal static class TextSimilarity
{
    private static readonly JaroWinkler JaroWinkler = new();

    public static double Of(string left, string right) =>
        left.Length == 0 || right.Length == 0 ? 0 : JaroWinkler.Similarity(left, right);
}
