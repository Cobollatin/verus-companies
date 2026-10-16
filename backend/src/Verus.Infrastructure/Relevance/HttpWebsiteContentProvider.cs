using System.Text;
using AngleSharp.Html.Parser;
using Microsoft.Extensions.Logging;
using Verus.Application.Abstractions;
using Verus.Domain.Companies;
using Verus.Domain.Relevance;

namespace Verus.Infrastructure.Relevance;

/// <summary>
/// Fetches a website's home page (SSRF-guarded, size- and time-limited) and extracts brand metadata.
/// Any failure degrades to <c>null</c>: content inspection is best-effort evidence, never a 500.
/// </summary>
internal sealed partial class HttpWebsiteContentProvider(
    HttpClient httpClient,
    ILogger<HttpWebsiteContentProvider> logger
) : IWebsiteContentProvider
{
    public const int MaxContentBytes = 256 * 1024;

    private static readonly HtmlParser Parser = new();

    public async Task<WebsiteMetadata?> GetMetadataAsync(WebsiteUrl website, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(website);

        var homePage = new Uri(new Uri(website.Value), "/");

        try
        {
            using var response = await httpClient.GetAsync(
                homePage,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken
            );

            var mediaType = response.Content.Headers.ContentType?.MediaType;
            if (!response.IsSuccessStatusCode || mediaType is not ("text/html" or "application/xhtml+xml"))
            {
                LogUnusableResponse(logger, homePage, (int)response.StatusCode, mediaType ?? "none");
                return null;
            }

            var html = await ReadLimitedAsync(response.Content, cancellationToken);
            return Extract(html);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or TimeoutException)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                throw;
            }

            LogFetchFailed(logger, homePage, ex.Message);
            return null;
        }
    }

    internal static WebsiteMetadata Extract(string html)
    {
        using var document = Parser.ParseDocument(html);

        static string? Clean(string? value) =>
            string.IsNullOrWhiteSpace(value)
                ? null
                : string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

        return new WebsiteMetadata(
            Title: Clean(document.Title),
            SiteName: Clean(document.QuerySelector("meta[property='og:site_name']")?.GetAttribute("content")),
            ApplicationName: Clean(document.QuerySelector("meta[name='application-name']")?.GetAttribute("content"))
        );
    }

    private static async Task<string> ReadLimitedAsync(HttpContent content, CancellationToken cancellationToken)
    {
        await using var stream = await content.ReadAsStreamAsync(cancellationToken);

        var buffer = new byte[MaxContentBytes];
        var total = 0;
        int read;
        while (total < buffer.Length && (read = await stream.ReadAsync(buffer.AsMemory(total), cancellationToken)) > 0)
        {
            total += read;
        }

        var encoding = GetEncoding(content.Headers.ContentType?.CharSet);
        return encoding.GetString(buffer, 0, total);
    }

    private static Encoding GetEncoding(string? charset)
    {
        if (string.IsNullOrWhiteSpace(charset))
        {
            return Encoding.UTF8;
        }

        try
        {
            return Encoding.GetEncoding(charset.Trim('"'));
        }
        catch (ArgumentException)
        {
            return Encoding.UTF8;
        }
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Content of {Url} unusable: status {StatusCode}, media type {MediaType}"
    )]
    private static partial void LogUnusableResponse(ILogger logger, Uri url, int statusCode, string mediaType);

    [LoggerMessage(Level = LogLevel.Information, Message = "Fetching {Url} failed: {Reason}")]
    private static partial void LogFetchFailed(ILogger logger, Uri url, string reason);
}
