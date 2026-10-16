using System.Net;
using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Verus.Application.Abstractions;
using Verus.Infrastructure.Persistence;
using Verus.Infrastructure.Relevance;

namespace Verus.Infrastructure;

public static class DependencyInjection
{
    public const string InMemoryDatabaseNameKey = "Persistence:InMemoryDatabaseName";

    /// <summary>
    /// Registers persistence and outbound HTTP. This is the single swap point for a real database:
    /// replace <c>UseInMemoryDatabase</c> with e.g. <c>UseNpgsql</c> and add migrations.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var databaseName = configuration[InMemoryDatabaseNameKey] ?? "Verus";
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(databaseName));
        services.AddScoped<ICompanyRepository, CompanyRepository>();

        services
            .AddHttpClient<IWebsiteContentProvider, HttpWebsiteContentProvider>(client =>
            {
                client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("VerusCompanyBot", "1.0"));
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
                client.MaxResponseContentBufferSize = HttpWebsiteContentProvider.MaxContentBytes;
            })
            .ConfigurePrimaryHttpMessageHandler(() =>
                new SocketsHttpHandler
                {
                    ConnectCallback = PublicAddressGuard.ConnectAsync,
                    AllowAutoRedirect = true,
                    MaxAutomaticRedirections = 3,
                    AutomaticDecompression = DecompressionMethods.All,
                    UseCookies = false,
                }
            )
            .AddStandardResilienceHandler(options =>
            {
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(3);
                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(7);
                options.Retry.MaxRetryAttempts = 1;
                options.Retry.ShouldHandle = args =>
                    ValueTask.FromResult(
                        args.Outcome.Exception?.GetBaseException() is not UnsafeAddressException
                            && HttpClientResiliencePredicates.IsTransient(args.Outcome)
                    );
                options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(10);
            });

        return services;
    }
}
