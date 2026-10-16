using Verus.Domain.Companies;
using Verus.Domain.Relevance;

namespace Verus.Application.Abstractions;

/// <summary>Reads brand metadata from a website's home page.</summary>
public interface IWebsiteContentProvider
{
    /// <returns>The metadata, or <c>null</c> when the site is unreachable, unsafe to fetch, or not HTML.</returns>
    Task<WebsiteMetadata?> GetMetadataAsync(WebsiteUrl website, CancellationToken cancellationToken);
}
