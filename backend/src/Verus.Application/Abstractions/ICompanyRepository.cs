using Verus.Application.Common.Paging;
using Verus.Domain.Companies;

namespace Verus.Application.Abstractions;

/// <summary>Persistence port for the <see cref="Company"/> aggregate. Implementations never leak storage types.</summary>
public interface ICompanyRepository
{
    Task AddAsync(Company company, CancellationToken cancellationToken);

    Task<Company?> GetByIdAsync(CompanyId id, CancellationToken cancellationToken);

    Task<bool> ExistsWithRegistrableDomainAsync(string registrableDomain, CancellationToken cancellationToken);

    /// <summary>Filters, sorts and pages in the store (database-friendly path).</summary>
    Task<PagedResult<Company>> ListAsync(
        CompanyFilter filter,
        CompanySort sort,
        PageRequest page,
        CancellationToken cancellationToken
    );

    /// <summary>Returns every company matching <paramref name="filter"/>, for in-memory relevance ranking.</summary>
    Task<IReadOnlyList<Company>> ListAllAsync(CompanyFilter filter, CancellationToken cancellationToken);
}
