using System.ComponentModel.DataAnnotations;

namespace Verus.Application.Common.Relevance;

/// <summary>Configuration section <c>Relevance</c>.</summary>
public sealed class RelevanceOptions
{
    public const string SectionName = "Relevance";

    /// <summary>Minimum score in [0, 1] for a company name to be considered relevant to its website.</summary>
    [Range(0.0, 1.0)]
    public double Threshold { get; init; } = 0.8;

    /// <summary>When the domain does not match, fetch the home page and look for the name in its metadata.</summary>
    public bool EnableContentInspection { get; init; } = true;
}
