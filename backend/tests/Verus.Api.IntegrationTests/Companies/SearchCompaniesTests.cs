using System.Net;
using Verus.Api.IntegrationTests.Infrastructure;

namespace Verus.Api.IntegrationTests.Companies;

public sealed class SearchCompaniesTests(SearchCompaniesTests.SeededApi api)
    : IClassFixture<SearchCompaniesTests.SeededApi>
{
    private readonly HttpClient _client = api.Client;

    [Fact]
    public async Task Get_WithoutParameters_ReturnsAllCompaniesSortedByName()
    {
        var page = await SearchAsync("");

        page.TotalCount.ShouldBe(SeededApi.Companies.Length);
        page.Items.Select(c => c.Name)
            .ShouldBe(SeededApi.Companies.Select(c => c.Name).Order(StringComparer.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("?name=micro", new[] { "Micron Technology", "Microsoft" })]
    [InlineData("?name=MICROSOFT", new[] { "Microsoft" })]
    [InlineData("?domain=microsoft.com", new[] { "Microsoft" })]
    [InlineData("?domain=https://www.contoso.co.uk/about", new[] { "Contoso" })]
    [InlineData("?domain=co.uk", new[] { "Contoso" })]
    [InlineData("?domain=soft", new[] { "Microsoft", "Soft Kitty Toys" })]
    [InlineData("?name=micro&domain=micron.com", new[] { "Micron Technology" })]
    public async Task Get_WithFilters_ReturnsMatchingCompanies(string query, string[] expected)
    {
        var page = await SearchAsync(query);

        page.Items.Select(c => c.Name).ShouldBe(expected);
    }

    [Fact]
    public async Task Get_WithQ_OrdersByRelevanceAndIncludesScores()
    {
        var page = await SearchAsync("?q=microsoft");

        page.Items[0].Name.ShouldBe("Microsoft");
        page.Items.ShouldAllBe(c => c.RelevanceScore > 0);
        page.Items.Select(c => c.RelevanceScore).ShouldBeInOrder(SortDirection.Descending);
        page.Items.ShouldNotContain(c => c.Name == "Soft Kitty Toys");
    }

    [Fact]
    public async Task Get_WithNoMatches_Returns200AndEmptyPage()
    {
        var page = await SearchAsync("?q=zzzzzz");

        page.Items.ShouldBeEmpty();
        page.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task Get_WithPaging_ReturnsRequestedSlice()
    {
        var page = await SearchAsync("?sort=-name&page=2&pageSize=2");

        page.ShouldSatisfyAllConditions(
            p => p.Items.Select(c => c.Name).ShouldBe(["Microsoft", "Micron Technology"]),
            p => p.Page.ShouldBe(2),
            p => p.TotalPages.ShouldBe(3),
            p => p.HasPreviousPage.ShouldBeTrue(),
            p => p.HasNextPage.ShouldBeTrue()
        );
    }

    [Theory]
    [InlineData("?pageSize=0", "pageSize")]
    [InlineData("?pageSize=500", "pageSize")]
    [InlineData("?page=abc", "page")]
    [InlineData("?sort=price", "sort")]
    [InlineData("?sort=relevance", "sort")]
    [InlineData("?domain=not a domain", "domain")]
    public async Task Get_WithInvalidQuery_Returns400(string query, string field)
    {
        var response = await _client.GetAsync(
            HttpExtensions.CompaniesRoute + query,
            TestContext.Current.CancellationToken
        );

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.ReadAsync<ProblemDto>()).Errors.ShouldContain(e => e.Name == field);
    }

    private async Task<PageDto> SearchAsync(string query)
    {
        var response = await _client.GetAsync(
            HttpExtensions.CompaniesRoute + query,
            TestContext.Current.CancellationToken
        );
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return await response.ReadAsync<PageDto>();
    }

    /// <summary>One API instance seeded with a fixed set of companies, shared by the read-only tests above.</summary>
    public sealed class SeededApi : IAsyncLifetime
    {
        public static readonly (string Name, string Url)[] Companies =
        [
            ("Microsoft", "https://www.microsoft.com"),
            ("Micron Technology", "https://www.micron.com"),
            ("Contoso", "https://www.contoso.co.uk"),
            ("Soft Kitty Toys", "https://softkitty.com"),
            ("Acme Corporation", "https://acme.com"),
            ("Zeta Systems", "https://zetasystems.io"),
        ];

        private readonly ApiFactory _factory = new();

        public HttpClient Client { get; private set; } = null!;

        public async ValueTask InitializeAsync()
        {
            Client = _factory.CreateClient();
            foreach (var (name, url) in Companies)
            {
                (await Client.CreateCompanyAsync(name, url)).EnsureSuccessStatusCode();
            }
        }

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await _factory.DisposeAsync();
        }
    }
}
