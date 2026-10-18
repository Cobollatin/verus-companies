namespace Verus.Application.Features.Companies.SearchCompanies;

/// <summary>Accepts <c>example.com</c>, <c>www.example.com</c> or a pasted URL and reduces it to a host fragment.</summary>
internal static class DomainQuery
{
    private const string SchemeSeparator = "://";
    private const string WwwPrefix = "www.";

    public static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var domain = WithoutWww(WithoutPath(WithoutScheme(value.Trim().ToLowerInvariant()))).Trim('.');

        return domain.Length > 0 && domain.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '.') ? domain : null;
    }

    private static string WithoutScheme(string text) =>
        text.IndexOf(SchemeSeparator, StringComparison.Ordinal) is var end and >= 0
            ? text[(end + SchemeSeparator.Length)..]
            : text;

    private static string WithoutPath(string text) =>
        text.IndexOfAny(['/', '?', '#', ':']) is var start and >= 0 ? text[..start] : text;

    private static string WithoutWww(string text) =>
        text.StartsWith(WwwPrefix, StringComparison.Ordinal) ? text[WwwPrefix.Length..] : text;
}
