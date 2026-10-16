using ErrorOr;
using Mediator;
using Verus.Application.Abstractions;
using Verus.Domain.Companies;

namespace Verus.Application.Features.Companies.GetCompanyById;

public sealed record GetCompanyByIdQuery(Guid Id) : IQuery<ErrorOr<CompanyResponse>>;

public sealed class GetCompanyByIdHandler(ICompanyRepository repository)
    : IQueryHandler<GetCompanyByIdQuery, ErrorOr<CompanyResponse>>
{
    public async ValueTask<ErrorOr<CompanyResponse>> Handle(
        GetCompanyByIdQuery query,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var id = new CompanyId(query.Id);
        var company = await repository.GetByIdAsync(id, cancellationToken);

        return company is null ? CompanyErrors.NotFound(id) : company.ToResponse();
    }
}
