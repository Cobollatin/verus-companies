using Microsoft.EntityFrameworkCore;
using Verus.Application.Abstractions;
using Verus.Application.Common.Paging;
using Verus.Domain.Companies;

namespace Verus.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of <see cref="ICompanyRepository"/>. All queries are plain LINQ that relational
/// providers translate, so switching from InMemory to a real database only changes <c>AddInfrastructure</c>.
/// </summary>
internal sealed class CompanyRepository(AppDbContext db) : ICompanyRepository
{
    public async Task AddAsync(Company company, CancellationToken cancellationToken)
    {
        db.Companies.Add(company);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task<Company?> GetByIdAsync(CompanyId id, CancellationToken cancellationToken) =>
        db.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<bool> ExistsWithRegistrableDomainAsync(string registrableDomain, CancellationToken cancellationToken) =>
        db.Companies.AnyAsync(c => c.Website.RegistrableDomain == registrableDomain, cancellationToken);

    public async Task<PagedResult<Company>> ListAsync(
        CompanyFilter filter,
        CompanySort sort,
        PageRequest page,
        CancellationToken cancellationToken
    )
    {
        var query = Filter(db.Companies.AsNoTracking(), filter);

        var total = await query.CountAsync(cancellationToken);
        var items = await Sort(query, sort).Skip(page.Skip).Take(page.PageSize).ToListAsync(cancellationToken);

        return new PagedResult<Company>(items, page.Page, page.PageSize, total);
    }

    public async Task<IReadOnlyList<Company>> ListAllAsync(CompanyFilter filter, CancellationToken cancellationToken) =>
        await Filter(db.Companies.AsNoTracking(), filter).ToListAsync(cancellationToken);

    // string.ToLower() (not ToLowerInvariant / StringComparison overloads) is what relational providers
    // translate to LOWER(); a real database would use a case-insensitive collation instead.
#pragma warning disable CA1304, CA1311, CA1862, MA0011
    private static IQueryable<Company> Filter(IQueryable<Company> query, CompanyFilter filter)
    {
        if (!string.IsNullOrWhiteSpace(filter.NameContains))
        {
            var term = filter.NameContains.ToLowerInvariant();
            query = query.Where(c => c.Name.Value.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(filter.Domain))
        {
            var domain = filter.Domain;
            if (domain.Contains('.', StringComparison.Ordinal))
            {
                var subdomainSuffix = "." + domain;
                query = query.Where(c =>
                    c.Website.Host == domain
                    || c.Website.RegistrableDomain == domain
                    || c.Website.Host.EndsWith(subdomainSuffix)
                );
            }
            else
            {
                query = query.Where(c => c.Website.Host.Contains(domain));
            }
        }

        return query;
    }

    private static IOrderedQueryable<Company> Sort(IQueryable<Company> query, CompanySort sort) =>
        sort switch
        {
            CompanySort.Name => query.OrderBy(c => c.Name.Value.ToLower()).ThenBy(c => c.Id),
            CompanySort.NameDescending => query.OrderByDescending(c => c.Name.Value.ToLower()).ThenBy(c => c.Id),
            CompanySort.CreatedAt => query.OrderBy(c => c.CreatedAt).ThenBy(c => c.Id),
            CompanySort.CreatedAtDescending => query.OrderByDescending(c => c.CreatedAt).ThenBy(c => c.Id),
            _ => throw new ArgumentOutOfRangeException(nameof(sort), sort, "Unsupported sort."),
        };
#pragma warning restore CA1304, CA1311, CA1862, MA0011
}
