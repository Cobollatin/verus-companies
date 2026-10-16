using System.Collections.Concurrent;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Verus.Application.Abstractions;
using Verus.Domain.Companies;
using Verus.Domain.Relevance;

namespace Verus.Api.IntegrationTests.Infrastructure;

/// <summary>Boots the real API in memory with an isolated database and a fake website-content provider.</summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public FakeWebsiteContentProvider Websites { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Persistence:InMemoryDatabaseName", $"tests-{Guid.NewGuid():N}");
        builder.UseSetting("Serilog:MinimumLevel:Default", "Warning");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IWebsiteContentProvider>();
            services.AddSingleton<IWebsiteContentProvider>(Websites);
        });
    }
}

/// <summary>Serves canned home-page metadata per host; unknown hosts behave like unreachable sites.</summary>
public sealed class FakeWebsiteContentProvider : IWebsiteContentProvider
{
    private readonly ConcurrentDictionary<string, WebsiteMetadata> _sites = new(StringComparer.OrdinalIgnoreCase);

    public void Add(string host, string title) => _sites[host] = new WebsiteMetadata(title, null, null);

    public Task<WebsiteMetadata?> GetMetadataAsync(WebsiteUrl website, CancellationToken cancellationToken) =>
        Task.FromResult(_sites.TryGetValue(website.Host, out var metadata) ? metadata : null);
}
