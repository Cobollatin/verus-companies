using ErrorOr;
using Mediator;

namespace Verus.Application.Features.Companies.CreateCompany;

public sealed record CreateCompanyCommand(string? Name, string? WebsiteUrl) : ICommand<ErrorOr<CompanyResponse>>;
