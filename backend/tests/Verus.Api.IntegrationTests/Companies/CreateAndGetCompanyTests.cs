using System.Net;
using Verus.Api.IntegrationTests.Infrastructure;

namespace Verus.Api.IntegrationTests.Companies;

public sealed class CreateAndGetCompanyTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Post_WithValidCompany_Returns201WithLocationThatResolves()
    {
        var response = await _client.CreateCompanyAsync("Contoso Ltd", "https://www.contoso.com/");

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = await response.ReadAsync<CompanyDto>();
        created.ShouldSatisfyAllConditions(
            c => c.Name.ShouldBe("Contoso Ltd"),
            c => c.WebsiteUrl.ShouldBe("https://www.contoso.com"),
            c => c.Domain.ShouldBe("contoso.com"),
            c => c.RelevanceScore.ShouldBeNull()
        );
        response.Headers.Location!.ToString().ShouldBe($"{HttpExtensions.CompaniesRoute}/{created.Id}");

        var fetched = await _client.GetAsync(response.Headers.Location, Ct);
        fetched.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await fetched.ReadAsync<CompanyDto>()).ShouldBe(created);
    }

    [Theory]
    [InlineData(null, "https://example.com", "name", "Company.Name.Required")]
    [InlineData("Ab", "https://ab.com", "name", "Company.Name.TooShort")]
    [InlineData("Example", null, "websiteUrl", "Company.Website.Required")]
    [InlineData("Example", "example.com", "websiteUrl", "Company.Website.Invalid")]
    [InlineData("Example", "ftp://example.com", "websiteUrl", "Company.Website.SchemeNotSupported")]
    [InlineData("Example", "https://localhost", "websiteUrl", "Company.Website.UnknownDomain")]
    public async Task Post_WithInvalidInput_Returns400ProblemDetails(
        string? name,
        string? website,
        string field,
        string code
    )
    {
        var response = await _client.CreateCompanyAsync(name, website);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
        var problem = await response.ReadAsync<ProblemDto>();
        problem.Status.ShouldBe(400);
        problem.Detail.ShouldNotBeNullOrWhiteSpace();
        problem.Errors.ShouldContain(e => e.Name == field && e.Code == code);
    }

    [Fact]
    public async Task Post_WithMultipleInvalidFields_ReturnsAllErrors()
    {
        var response = await _client.CreateCompanyAsync("", "nope");

        var problem = await response.ReadAsync<ProblemDto>();
        problem.Errors.Select(e => e.Name).ShouldBe(["name", "websiteUrl"], ignoreOrder: true);
    }

    [Fact]
    public async Task Post_WhenNameIsNotRelevantToWebsite_Returns400WithExplanation()
    {
        var response = await _client.CreateCompanyAsync("Apple", "https://www.fabrikam.com");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.ReadAsync<ProblemDto>();
        problem.Errors.ShouldHaveSingleItem().Code.ShouldBe("Company.Website.NotRelevant");
        problem.Detail.ShouldContain("fabrikam.com");
    }

    [Fact]
    public async Task Post_WhenOnlyWebsiteContentNamesTheCompany_Returns201()
    {
        factory.Websites.Add("abc.xyz", "Alphabet");

        var response = await _client.CreateCompanyAsync("Alphabet Inc.", "https://abc.xyz");

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Post_WithAlreadyRegisteredDomain_Returns409()
    {
        (await _client.CreateCompanyAsync("Northwind", "https://northwind.com")).EnsureSuccessStatusCode();

        var response = await _client.CreateCompanyAsync("Northwind Traders", "https://shop.northwind.com");

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await response.ReadAsync<ProblemDto>()).Errors.Single().Code.ShouldBe("Company.Website.Duplicate");
    }

    [Fact]
    public async Task Post_WithMalformedJson_Returns400()
    {
        using var content = new StringContent("{\"name\":", System.Text.Encoding.UTF8, "application/json");

        var response = await _client.PostAsync(HttpExtensions.CompaniesRoute, content, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.ReadAsync<ProblemDto>()).Detail.ShouldBe("The request body is not valid JSON.");
    }

    [Fact]
    public async Task Get_WithUnknownId_Returns404ProblemDetails()
    {
        var response = await _client.GetAsync($"{HttpExtensions.CompaniesRoute}/{Guid.NewGuid()}", Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await response.ReadAsync<ProblemDto>()).Errors.Single().Code.ShouldBe("Company.NotFound");
    }

    [Fact]
    public async Task Get_WithMalformedId_Returns400()
    {
        var response = await _client.GetAsync($"{HttpExtensions.CompaniesRoute}/not-a-guid", Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.ReadAsync<ProblemDto>()).Errors.Single().Name.ShouldBe("id");
    }
}
