using NSubstitute;
using Verus.Application.Abstractions;
using Verus.Application.Common.Paging;
using Verus.Application.Features.Companies.SearchCompanies;
using Verus.Domain.Companies;

namespace Verus.Application.Tests.Features.Companies;

public sealed class SearchCompaniesTests
{
    private readonly ICompanyRepository _repository = Substitute.For<ICompanyRepository>();
    private readonly SearchCompaniesHandler _handler;

    public SearchCompaniesTests()
    {
        _handler = new SearchCompaniesHandler(_repository);
    }

    [Fact]
    public async Task Handle_WithoutQuery_DelegatesFilteringSortingAndPagingToRepository()
    {
        _repository
            .ListAsync(
                Arg.Any<CompanyFilter>(),
                Arg.Any<CompanySort>(),
                Arg.Any<PageRequest>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(new PagedResult<Company>([TestData.Company("Acme", "https://acme.com")], 2, 5, 6));

        var result = await _handler.Handle(
            new SearchCompaniesQuery(
                Name: " ac ",
                Domain: "https://www.Acme.com/about",
                Sort: "-createdAt",
                Page: 2,
                PageSize: 5
            ),
            CancellationToken.None
        );

        result.Value.ShouldSatisfyAllConditions(
            r => r.Items.Single().Name.ShouldBe("Acme"),
            r => r.TotalCount.ShouldBe(6),
            r => r.TotalPages.ShouldBe(2)
        );
        await _repository
            .Received(1)
            .ListAsync(
                new CompanyFilter("ac", "acme.com"),
                CompanySort.CreatedAtDescending,
                new PageRequest(2, 5),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task Handle_WithQuery_RanksByRelevanceAndExcludesNonMatches()
    {
        _repository
            .ListAllAsync(Arg.Any<CompanyFilter>(), Arg.Any<CancellationToken>())
            .Returns([
                TestData.Company("Micron Technology", "https://micron.com"),
                TestData.Company("Banana Republic", "https://bananarepublic.com"),
                TestData.Company("Microsoft", "https://microsoft.com"),
            ]);

        var result = await _handler.Handle(new SearchCompaniesQuery(Q: "microsoft"), CancellationToken.None);

        result.Value.Items.Select(i => i.Name).ShouldBe(["Microsoft", "Micron Technology"]);
        result.Value.Items[0].RelevanceScore.ShouldBe(1.0);
        result.Value.TotalCount.ShouldBe(2);
    }

    [Fact]
    public async Task Handle_WithQueryAndNoMatches_ReturnsEmptyPage()
    {
        _repository.ListAllAsync(Arg.Any<CompanyFilter>(), Arg.Any<CancellationToken>()).Returns([]);

        var result = await _handler.Handle(new SearchCompaniesQuery(Q: "zzz"), CancellationToken.None);

        result.IsError.ShouldBeFalse();
        result.Value.Items.ShouldBeEmpty();
        result.Value.TotalCount.ShouldBe(0);
    }

    [Theory]
    [InlineData("example.com", "example.com")]
    [InlineData("  WWW.Example.COM  ", "example.com")]
    [InlineData("https://shop.example.com:8080/path?x=1", "shop.example.com")]
    [InlineData("example", "example")]
    [InlineData("", null)]
    [InlineData("exa mple", null)]
    public void DomainQuery_Normalize(string input, string? expected) =>
        DomainQuery.Normalize(input).ShouldBe(expected);

    [Theory]
    [InlineData(null, null, 1, 20, true)]
    [InlineData("name", null, 1, 20, true)]
    [InlineData("-createdAt", null, 1, 100, true)]
    [InlineData("relevance", "acme", 1, 20, true)]
    [InlineData("relevance", null, 1, 20, false)]
    [InlineData("price", null, 1, 20, false)]
    [InlineData(null, null, 0, 20, false)]
    [InlineData(null, null, 1, 0, false)]
    [InlineData(null, null, 1, 101, false)]
    public void Validator_ChecksSortAndPaging(string? sort, string? q, int page, int pageSize, bool valid) =>
        new SearchCompaniesValidator()
            .Validate(new SearchCompaniesQuery(Q: q, Sort: sort, Page: page, PageSize: pageSize))
            .IsValid.ShouldBe(valid);
}
