namespace Verus.Domain.Relevance;

/// <summary>How a relevance verdict was reached.</summary>
public enum RelevanceMethod
{
    None,
    Domain,
    Content,
}

/// <summary>Explainable verdict on whether a company name belongs to a website.</summary>
/// <param name="IsRelevant">Whether <paramref name="Score"/> met the policy threshold.</param>
/// <param name="Score">Confidence in [0, 1].</param>
/// <param name="Method">Which strategy produced the verdict.</param>
/// <param name="Reason">Human-readable explanation, safe to show to end users.</param>
public sealed record RelevanceAssessment(bool IsRelevant, double Score, RelevanceMethod Method, string Reason)
{
    public static RelevanceAssessment From(RelevanceCandidate candidate, RelevanceMethod method, RelevancePolicy policy)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(policy);
        return new(candidate.Score >= policy.Threshold, candidate.Score, method, candidate.Reason);
    }
}

/// <summary>A single scored piece of evidence produced by a matching rule.</summary>
public sealed record RelevanceCandidate(double Score, string Reason)
{
    public static readonly RelevanceCandidate None = new(0, "No evidence found.");
}

/// <summary>Tunable policy for relevance decisions.</summary>
/// <param name="Threshold">Minimum score in [0, 1] for a name to be considered relevant.</param>
public sealed record RelevancePolicy(double Threshold)
{
    public static readonly RelevancePolicy Default = new(0.8);
}
