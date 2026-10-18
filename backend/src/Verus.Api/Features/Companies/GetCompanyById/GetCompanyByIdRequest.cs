namespace Verus.Api.Features.Companies.GetCompanyById;

/// <summary>Route parameters for fetching one company.</summary>
public sealed class GetCompanyByIdRequest
{
    /// <summary>Company identifier (GUID).</summary>
    public Guid Id { get; init; }
}
