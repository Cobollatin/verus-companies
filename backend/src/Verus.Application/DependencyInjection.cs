using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Verus.Application.Common.Relevance;

namespace Verus.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registers application services. Mediator handlers and pipeline behaviors are registered by the
    /// source-generated <c>AddMediator</c> in the composition root.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<RelevanceOptions>(
            ServiceLifetime.Singleton,
            includeInternalTypes: true
        );

        services
            .AddOptions<RelevanceOptions>()
            .BindConfiguration(RelevanceOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddScoped<IRelevanceEvaluator, RelevanceEvaluator>();
        services.AddSingleton(TimeProvider.System);

        return services;
    }
}
