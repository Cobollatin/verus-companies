using Verus.Application.Abstractions;

namespace Verus.Application.Features.Companies.SearchCompanies;

/// <summary>Parses the <c>sort</c> query parameter. <c>null</c> sort means "by relevance".</summary>
internal static class SortOrder
{
    public const string Relevance = "relevance";

    private static readonly Dictionary<string, CompanySort?> Map = new(StringComparer.OrdinalIgnoreCase)
    {
        [Relevance] = null,
        ["name"] = CompanySort.Name,
        ["-name"] = CompanySort.NameDescending,
        ["createdAt"] = CompanySort.CreatedAt,
        ["-createdAt"] = CompanySort.CreatedAtDescending,
    };

    public static IEnumerable<string> Allowed => Map.Keys;

    public static bool IsRelevance(string? value) =>
        string.Equals(value?.Trim(), Relevance, StringComparison.OrdinalIgnoreCase);

    /// <summary>Parses a value already accepted by <see cref="TryParse"/>.</summary>
    public static CompanySort? Parse(string? value) => string.IsNullOrWhiteSpace(value) ? null : Map[value.Trim()];

    public static bool TryParse(string? value, out CompanySort? sort)
    {
        sort = null;
        return string.IsNullOrWhiteSpace(value) || Map.TryGetValue(value.Trim(), out sort);
    }
}
