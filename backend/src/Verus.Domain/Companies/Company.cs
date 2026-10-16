using ErrorOr;
using Verus.Domain.Relevance;

namespace Verus.Domain.Companies;

/// <summary>
/// Aggregate root. A <see cref="Company"/> can only exist with a valid name, a valid website,
/// and a positive relevance assessment that the name belongs to that website.
/// </summary>
public sealed class Company
{
    // Required by EF Core for materialization.
    private Company() { }

    private Company(CompanyId id, CompanyName name, WebsiteUrl website, DateTimeOffset createdAt)
    {
        Id = id;
        Name = name;
        Website = website;
        CreatedAt = createdAt;
    }

    public CompanyId Id { get; private set; }

    public CompanyName Name { get; private set; } = null!;

    public WebsiteUrl Website { get; private set; } = null!;

    public DateTimeOffset CreatedAt { get; private set; }

    public static ErrorOr<Company> Create(
        CompanyName name,
        WebsiteUrl website,
        RelevanceAssessment relevance,
        TimeProvider clock
    )
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(website);
        ArgumentNullException.ThrowIfNull(relevance);
        ArgumentNullException.ThrowIfNull(clock);

        if (!relevance.IsRelevant)
        {
            return CompanyErrors.NotRelevant(relevance);
        }

        return new Company(CompanyId.New(), name, website, clock.GetUtcNow());
    }
}
