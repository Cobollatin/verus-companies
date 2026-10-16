using ErrorOr;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Verus.Application.Abstractions;
using Verus.Application.Common.Relevance;
using Verus.Application.Features.Companies.CreateCompany;
using Verus.Domain.Companies;
using Verus.Domain.Relevance;

namespace Verus.Application.Tests.Features.Companies;

public sealed class CreateCompanyHandlerTests
{
    private readonly ICompanyRepository _repository = Substitute.For<ICompanyRepository>();
    private readonly IRelevanceEvaluator _relevance = Substitute.For<IRelevanceEvaluator>();
    private readonly CreateCompanyHandler _handler;

    public CreateCompanyHandlerTests()
    {
        _handler = new CreateCompanyHandler(
            _repository,
            _relevance,
            new FakeTimeProvider(TestData.Now),
            NullLogger<CreateCompanyHandler>.Instance
        );
    }

    [Fact]
    public async Task Handle_WithRelevantCompany_PersistsAndReturnsIt()
    {
        GivenRelevance(isRelevant: true);

        var result = await _handler.Handle(
            new CreateCompanyCommand("  Acme Corp ", "https://www.acme.com/"),
            CancellationToken.None
        );

        result.IsError.ShouldBeFalse();
        result.Value.ShouldSatisfyAllConditions(
            r => r.Id.ShouldNotBe(Guid.Empty),
            r => r.Name.ShouldBe("Acme Corp"),
            r => r.WebsiteUrl.ShouldBe("https://www.acme.com"),
            r => r.Domain.ShouldBe("acme.com"),
            r => r.CreatedAt.ShouldBe(TestData.Now),
            r => r.RelevanceScore.ShouldBeNull()
        );
        await _repository
            .Received(1)
            .AddAsync(Arg.Is<Company>(c => c.Name.Value == "Acme Corp"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenNotRelevant_ReturnsValidationErrorAndDoesNotPersist()
    {
        GivenRelevance(isRelevant: false);

        var result = await _handler.Handle(
            new CreateCompanyCommand("Apple", "https://www.microsoft.com"),
            CancellationToken.None
        );

        result.FirstError.Code.ShouldBe("Company.Website.NotRelevant");
        result.FirstError.Type.ShouldBe(ErrorType.Validation);
        await _repository.DidNotReceive().AddAsync(Arg.Any<Company>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenDomainAlreadyRegistered_ReturnsConflict()
    {
        _repository.ExistsWithRegistrableDomainAsync("acme.com", Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.Handle(
            new CreateCompanyCommand("Acme", "https://shop.acme.com"),
            CancellationToken.None
        );

        result.FirstError.Type.ShouldBe(ErrorType.Conflict);
        await _relevance
            .DidNotReceive()
            .EvaluateAsync(Arg.Any<CompanyName>(), Arg.Any<WebsiteUrl>(), Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().AddAsync(Arg.Any<Company>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithInvalidInput_ReturnsEveryFieldError()
    {
        var result = await _handler.Handle(new CreateCompanyCommand("", "nope"), CancellationToken.None);

        result.Errors.Select(e => e.Code).ShouldBe(["Company.Name.Required", "Company.Website.Invalid"]);
    }

    private void GivenRelevance(bool isRelevant) =>
        _relevance
            .EvaluateAsync(Arg.Any<CompanyName>(), Arg.Any<WebsiteUrl>(), Arg.Any<CancellationToken>())
            .Returns(new RelevanceAssessment(isRelevant, isRelevant ? 1 : 0.1, RelevanceMethod.Domain, "Because."));
}
