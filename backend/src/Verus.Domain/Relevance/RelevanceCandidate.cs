namespace Verus.Domain.Relevance;

/// <summary>A single scored piece of evidence produced by a matching rule.</summary>
public sealed record RelevanceCandidate(double Score, string Reason)
{
    public static readonly RelevanceCandidate None = new(0, "No evidence found.");
}
