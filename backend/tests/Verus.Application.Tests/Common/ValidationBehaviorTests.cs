using ErrorOr;
using Verus.Application.Common.Behaviors;
using Verus.Application.Features.Companies;
using Verus.Application.Features.Companies.CreateCompany;
using Verus.Domain.Common;

namespace Verus.Application.Tests.Common;

public sealed class ValidationBehaviorTests
{
    private readonly ValidationBehavior<CreateCompanyCommand, ErrorOr<CompanyResponse>> _behavior = new([
        new CreateCompanyValidator(),
    ]);

    private bool _nextCalled;

    [Fact]
    public async Task Handle_WithInvalidMessage_ShortCircuitsWithFieldErrors()
    {
        var result = await _behavior.Handle(new CreateCompanyCommand("A", "ftp://x.com"), Next, CancellationToken.None);

        _nextCalled.ShouldBeFalse();
        result.IsError.ShouldBeTrue();
        result.Errors.ShouldAllBe(e => e.Type == ErrorType.Validation);
        result
            .Errors.Select(e => (e.GetField(), e.Code))
            .ShouldBe([("name", "Company.Name.TooShort"), ("websiteUrl", "Company.Website.SchemeNotSupported")]);
    }

    [Fact]
    public async Task Handle_WithValidMessage_CallsNext()
    {
        var result = await _behavior.Handle(
            new CreateCompanyCommand("Acme", "https://acme.com"),
            Next,
            CancellationToken.None
        );

        _nextCalled.ShouldBeTrue();
        result.IsError.ShouldBeFalse();
    }

    private ValueTask<ErrorOr<CompanyResponse>> Next(CreateCompanyCommand message, CancellationToken ct)
    {
        _nextCalled = true;
        return ValueTask.FromResult<ErrorOr<CompanyResponse>>(
            new CompanyResponse(Guid.NewGuid(), message.Name!, message.WebsiteUrl!, "acme.com", TestData.Now)
        );
    }
}
