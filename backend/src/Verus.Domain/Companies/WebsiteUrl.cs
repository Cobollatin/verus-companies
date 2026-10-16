using ErrorOr;
using Verus.Domain.Internet;

namespace Verus.Domain.Companies;

/// <summary>
/// An absolute http(s) URL whose host is a real, registrable domain name
/// (e.g. <c>https://shop.example.co.uk</c> → registrable domain <c>example.co.uk</c>, label <c>example</c>).
/// </summary>
public sealed record WebsiteUrl
{
    public const int MaxLength = 2048;

    private WebsiteUrl(string value, string host, string registrableDomain, string domainLabel)
    {
        Value = value;
        Host = host;
        RegistrableDomain = registrableDomain;
        DomainLabel = domainLabel;
    }

    /// <summary>Normalized URL: lower-case scheme and host, no trailing slash on the root path.</summary>
    public string Value { get; }

    /// <summary>Full host name, e.g. <c>www.example.co.uk</c>.</summary>
    public string Host { get; }

    /// <summary>Domain registered with a registrar, e.g. <c>example.co.uk</c>.</summary>
    public string RegistrableDomain { get; }

    /// <summary>Registrable domain without its public suffix, e.g. <c>example</c>.</summary>
    public string DomainLabel { get; }

    public static ErrorOr<WebsiteUrl> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return CompanyErrors.WebsiteRequired;
        }

        var trimmed = value.Trim();

        if (trimmed.Length > MaxLength)
        {
            return CompanyErrors.WebsiteTooLong;
        }

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) || !uri.IsWellFormedOriginalString())
        {
            return CompanyErrors.WebsiteInvalid;
        }

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            return CompanyErrors.WebsiteSchemeNotSupported;
        }

        if (!string.IsNullOrEmpty(uri.UserInfo) || uri.HostNameType != UriHostNameType.Dns)
        {
            return CompanyErrors.WebsiteInvalid;
        }

        var host = uri.IdnHost.ToLowerInvariant();

        if (!PublicSuffix.TryParse(host, out var registrableDomain, out var label))
        {
            return CompanyErrors.WebsiteUnknownDomain;
        }

        return new WebsiteUrl(Normalize(uri), host, registrableDomain, label);
    }

    public override string ToString() => Value;

    private static string Normalize(Uri uri)
    {
        var normalized = uri.GetComponents(
            UriComponents.SchemeAndServer | UriComponents.PathAndQuery | UriComponents.Fragment,
            UriFormat.UriEscaped
        );

        return uri.AbsolutePath == "/" && string.IsNullOrEmpty(uri.Query) && string.IsNullOrEmpty(uri.Fragment)
            ? normalized.TrimEnd('/')
            : normalized;
    }
}
