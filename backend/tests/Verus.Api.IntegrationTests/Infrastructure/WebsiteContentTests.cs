using System.Net;
using Verus.Infrastructure.Relevance;

namespace Verus.Api.IntegrationTests.Infrastructure;

public sealed class WebsiteContentTests
{
    [Theory]
    [InlineData("8.8.8.8", true)]
    [InlineData("140.82.112.3", true)]
    [InlineData("2606:4700:4700::1111", true)]
    [InlineData("127.0.0.1", false)]
    [InlineData("10.1.2.3", false)]
    [InlineData("172.16.0.1", false)]
    [InlineData("172.31.255.255", false)]
    [InlineData("192.168.1.1", false)]
    [InlineData("169.254.169.254", false)]
    [InlineData("100.64.0.1", false)]
    [InlineData("0.0.0.0", false)]
    [InlineData("224.0.0.1", false)]
    [InlineData("::1", false)]
    [InlineData("fe80::1", false)]
    [InlineData("fd00::1", false)]
    [InlineData("::ffff:127.0.0.1", false)]
    public void PublicAddressGuard_AllowsOnlyPublicAddresses(string address, bool expected) =>
        PublicAddressGuard.IsPublic(IPAddress.Parse(address)).ShouldBe(expected);

    [Fact]
    public void Extract_ReadsTitleAndBrandMetaTags()
    {
        const string html = """
            <html><head>
              <title>
                Welcome to   Acme | Home
              </title>
              <meta property="og:site_name" content="Acme Corporation">
              <meta name="application-name" content="Acme">
            </head><body></body></html>
            """;

        var metadata = HttpWebsiteContentProvider.Extract(html);

        metadata.Title.ShouldBe("Welcome to Acme | Home");
        metadata.SiteName.ShouldBe("Acme Corporation");
        metadata.ApplicationName.ShouldBe("Acme");
    }

    [Fact]
    public void Extract_WithoutMetadata_ReturnsNulls()
    {
        var metadata = HttpWebsiteContentProvider.Extract("<p>hello</p>");

        metadata.ShouldBe(new(null, null, null));
    }
}
