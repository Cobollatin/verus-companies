using Verus.Domain.Companies;
using Verus.Domain.Relevance;

namespace Verus.Application.Common.Relevance;

/// <summary>Decides whether a company name belongs to a website.</summary>
public interface IRelevanceEvaluator
{
    Task<RelevanceAssessment> EvaluateAsync(CompanyName name, WebsiteUrl website, CancellationToken cancellationToken);
}
