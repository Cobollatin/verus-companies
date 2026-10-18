using FluentValidation;
using Verus.Application.Common.Validation;
using Verus.Domain.Companies;

namespace Verus.Application.Features.Companies.CreateCompany;

public sealed class CreateCompanyValidator : AbstractValidator<CreateCompanyCommand>
{
    public CreateCompanyValidator()
    {
        RuleFor(c => c.Name).MustBeValueObject(CompanyName.Create);
        RuleFor(c => c.WebsiteUrl).MustBeValueObject(WebsiteUrl.Create);
    }
}
