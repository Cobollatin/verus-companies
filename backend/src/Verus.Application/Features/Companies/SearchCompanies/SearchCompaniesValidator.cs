using FluentValidation;
using Verus.Application.Common.Paging;

namespace Verus.Application.Features.Companies.SearchCompanies;

public sealed class SearchCompaniesValidator : AbstractValidator<SearchCompaniesQuery>
{
    public const int MaxTermLength = 200;

    public SearchCompaniesValidator()
    {
        RuleFor(q => q.Name)
            .MaximumLength(MaxTermLength)
            .WithErrorCode("Search.Name.TooLong")
            .WithMessage($"Name filter must be at most {MaxTermLength} characters.");
        RuleFor(q => q.Q)
            .MaximumLength(MaxTermLength)
            .WithErrorCode("Search.Q.TooLong")
            .WithMessage($"Search query must be at most {MaxTermLength} characters.");
        RuleFor(q => q.Domain)
            .Must(d => d!.Length <= 253 && DomainQuery.Normalize(d) is not null)
            .When(q => !string.IsNullOrWhiteSpace(q.Domain))
            .WithErrorCode("Search.Domain.Invalid")
            .WithMessage("Domain must look like 'example.com' or 'example'.");
        RuleFor(q => q.Sort)
            .Must(s => SortOrder.TryParse(s, out _))
            .WithErrorCode("Search.Sort.Invalid")
            .WithMessage($"Sort must be one of: {string.Join(", ", SortOrder.Allowed)}.");
        RuleFor(q => q.Sort)
            .Must(s => !SortOrder.IsRelevance(s))
            .When(q => string.IsNullOrWhiteSpace(q.Q))
            .WithErrorCode("Search.Sort.RelevanceRequiresQuery")
            .WithMessage("Sort 'relevance' requires a search query 'q'.");
        RuleFor(q => q.Page)
            .GreaterThanOrEqualTo(1)
            .WithErrorCode("Search.Page.Invalid")
            .WithMessage("Page must be 1 or greater.");
        RuleFor(q => q.PageSize)
            .InclusiveBetween(1, PageRequest.MaxPageSize)
            .WithErrorCode("Search.PageSize.Invalid")
            .WithMessage($"Page size must be between 1 and {PageRequest.MaxPageSize}.");
    }
}
