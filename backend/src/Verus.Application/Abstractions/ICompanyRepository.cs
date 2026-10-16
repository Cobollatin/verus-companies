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

/// <summary>Structured filters. <c>null</c> means "no constraint".</summary>
/// <param name="NameContains">Case-insensitive partial or full company name.</param>
/// <param name="Domain">
/// A domain such as <c>example.com</c> (matches the host, the registrable domain or any subdomain),
/// or a fragment such as <c>example</c> (matches hosts containing it). Lower-case, without scheme or path.
/// </param>
public sealed record CompanyFilter(string? NameContains, string? Domain)
{
    public static readonly CompanyFilter None = new(null, null);
}

public enum CompanySort
{
    Name,
    NameDescending,
    CreatedAt,
    CreatedAtDescending,
}
