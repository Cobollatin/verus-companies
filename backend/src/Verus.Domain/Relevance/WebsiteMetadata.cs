namespace Verus.Domain.Relevance;

/// <summary>Brand metadata published by a website's home page.</summary>
/// <param name="Title">The HTML <c>&lt;title&gt;</c>.</param>
/// <param name="SiteName">The <c>og:site_name</c> meta tag.</param>
/// <param name="ApplicationName">The <c>application-name</c> meta tag.</param>
public sealed record WebsiteMetadata(string? Title, string? SiteName, string? ApplicationName);
