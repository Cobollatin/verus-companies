namespace Verus.Application.Abstractions;

/// <summary>Structured filters. <c>null</c> means "no constraint".</summary>
/// <param name="NameContains">Case-insensitive partial or full company name.</param>
/// <param name="Domain">
/// A domain such as <c>example.com</c> (matches the host, the registrable domain or any subdomain),
/// or a fragment such as <c>example</c> (matches hosts containing it). Lower-case, without scheme or path.
/// </param>
public sealed record CompanyFilter(string? NameContains, string? Domain)
{
    public static readonly CompanyFilter None = new(null, null);
}
