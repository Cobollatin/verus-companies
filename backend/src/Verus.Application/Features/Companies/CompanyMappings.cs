using Verus.Domain.Companies;

namespace Verus.Application.Features.Companies;

internal static class CompanyMappings
{
    public static CompanyResponse ToResponse(this Company company, double? relevanceScore = null) =>
        new(
            company.Id.Value,
            company.Name.Value,
            company.Website.Value,
            company.Website.RegistrableDomain,
            company.CreatedAt,
            relevanceScore
        );
}
