using ErrorOr;
using Verus.Domain.Companies;

namespace Verus.Domain.Tests.Companies;

public sealed class WebsiteUrlTests
{
    [Theory]
    [InlineData("https://www.example.com", "https://www.example.com", "www.example.com", "example.com", "example")]
    [InlineData("HTTPS://WWW.Example.COM/", "https://www.example.com", "www.example.com", "example.com", "example")]
    [InlineData(
        "http://example.com/about?x=1",
        "http://example.com/about?x=1",
        "example.com",
        "example.com",
        "example"
    )]
    [InlineData(
        "https://shop.example.co.uk/path",
        "https://shop.example.co.uk/path",
        "shop.example.co.uk",
        "example.co.uk",
        "example"
    )]
    [InlineData("https://acme.github.io", "https://acme.github.io", "acme.github.io", "acme.github.io", "acme")]
    public void Create_WithValidUrl_ExtractsDomainParts(
        string input,
        string value,
        string host,
        string registrableDomain,
        string label
    )
    {
        var result = WebsiteUrl.Create(input);

        result.IsError.ShouldBeFalse();
        result.Value.ShouldSatisfyAllConditions(
            url => url.Value.ShouldBe(value),
            url => url.Host.ShouldBe(host),
            url => url.RegistrableDomain.ShouldBe(registrableDomain),
            url => url.DomainLabel.ShouldBe(label)
        );
    }

    [Theory]
    [InlineData(null, "Company.Website.Required")]
    [InlineData(" ", "Company.Website.Required")]
    [InlineData("not a url", "Company.Website.Invalid")]
    [InlineData("www.example.com", "Company.Website.Invalid")]
    [InlineData("https://user:secret@example.com", "Company.Website.Invalid")]
    [InlineData("https://192.168.0.1", "Company.Website.Invalid")]
    [InlineData("ftp://example.com", "Company.Website.SchemeNotSupported")]
    [InlineData("mailto:someone@example.com", "Company.Website.SchemeNotSupported")]
    [InlineData("https://localhost", "Company.Website.UnknownDomain")]
    [InlineData("https://com", "Company.Website.UnknownDomain")]
    public void Create_WithInvalidUrl_ReturnsValidationError(string? input, string expectedCode)
    {
        var result = WebsiteUrl.Create(input);

        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(ErrorType.Validation);
        result.FirstError.Code.ShouldBe(expectedCode);
    }

    [Fact]
    public void Create_WithTooLongUrl_ReturnsTooLong() =>
        WebsiteUrl
            .Create("https://example.com/" + new string('a', WebsiteUrl.MaxLength))
            .FirstError.ShouldBe(CompanyErrors.WebsiteTooLong);
}
