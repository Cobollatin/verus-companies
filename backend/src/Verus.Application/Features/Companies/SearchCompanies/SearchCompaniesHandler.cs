using ErrorOr;
using FluentValidation;
using Mediator;
using Verus.Application.Abstractions;
using Verus.Application.Common.Paging;
using Verus.Domain.Companies;

namespace Verus.Application.Features.Companies.SearchCompanies;

public sealed class SearchCompaniesHandler(ICompanyRepository repository)
    : IQueryHandler<SearchCompaniesQuery, ErrorOr<PagedResult<CompanyResponse>>>
{
    public async ValueTask<ErrorOr<PagedResult<CompanyResponse>>> Handle(
        SearchCompaniesQuery query,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var filter = new CompanyFilter(
            string.IsNullOrWhiteSpace(query.Name) ? null : query.Name.Trim(),
            DomainQuery.Normalize(query.Domain)
        );
        var page = new PageRequest(query.Page, query.PageSize);
        var sort = SortOrder.Parse(query.Sort);

        if (string.IsNullOrWhiteSpace(query.Q))
        {
            var result = await repository.ListAsync(filter, sort ?? CompanySort.Name, page, cancellationToken);
            return result.Map(c => c.ToResponse());
        }

        // Relevance ranking runs in memory over the filtered candidates; a database implementation
        // would push this down to full-text search behind the same port.
        var candidates = await repository.ListAllAsync(filter, cancellationToken);
        var hits = candidates
            .Select(c => (Company: c, Score: CompanySearchScorer.Score(c, query.Q)))
            .Where(hit => hit.Score > 0);

        var ordered = sort switch
        {
            null => hits.OrderByDescending(h => h.Score)
                .ThenBy(h => h.Company.Name.Value, StringComparer.OrdinalIgnoreCase),
            CompanySort.Name => hits.OrderBy(h => h.Company.Name.Value, StringComparer.OrdinalIgnoreCase),
            CompanySort.NameDescending => hits.OrderByDescending(
                h => h.Company.Name.Value,
                StringComparer.OrdinalIgnoreCase
            ),
            CompanySort.CreatedAt => hits.OrderBy(h => h.Company.CreatedAt),
            CompanySort.CreatedAtDescending => hits.OrderByDescending(h => h.Company.CreatedAt),
            _ => throw new InvalidOperationException($"Unsupported sort {sort}."),
        };

        return PagedResult.From([.. ordered], page).Map(h => h.Company.ToResponse(Math.Round(h.Score, 4)));
    }
}
