using ErrorOr;
using Mediator;

namespace Verus.Application.Features.Companies.GetCompanyById;

public sealed record GetCompanyByIdQuery(Guid Id) : IQuery<ErrorOr<CompanyResponse>>;
