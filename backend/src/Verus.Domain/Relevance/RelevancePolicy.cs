namespace Verus.Domain.Relevance;

/// <summary>Tunable policy for relevance decisions.</summary>
/// <param name="Threshold">Minimum score in [0, 1] for a name to be considered relevant.</param>
public sealed record RelevancePolicy(double Threshold)
{
    public static readonly RelevancePolicy Default = new(0.8);
}
