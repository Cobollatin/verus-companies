using System.Globalization;
using System.Text.Json.Serialization;
using FastEndpoints;
using FastEndpoints.Swagger;
using Scalar.AspNetCore;
using Serilog;
using Verus.Api.Common;
using Verus.Application;
using Verus.Application.Common.Behaviors;
using Verus.Application.Features.Companies.CreateCompany;
using Verus.Infrastructure;

// Startup logger for failures before the host is built; the host gets its own configured logger.
Log.Logger = new LoggerConfiguration().WriteTo.Console(formatProvider: CultureInfo.InvariantCulture).CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddSerilog(
        (services, logger) =>
            logger.ReadFrom.Configuration(builder.Configuration).ReadFrom.Services(services).Enrich.FromLogContext(),
        preserveStaticLogger: true
    );

    builder.Services.AddApplication().AddInfrastructure(builder.Configuration);

    builder.Services.AddMediator(options =>
    {
        options.ServiceLifetime = ServiceLifetime.Scoped;
        options.Assemblies = [typeof(CreateCompanyCommand)];
        options.PipelineBehaviors = [typeof(LoggingBehavior<,>), typeof(ValidationBehavior<,>)];
    });

    builder
        .Services.AddFastEndpoints()
        .SwaggerDocument(options =>
        {
            options.MaxEndpointVersion = 1;
            // Schema names and operation ids from type names, not namespaces, so moving files keeps the contract.
            options.ShortSchemaNames = true;
            options.DocumentSettings = settings =>
            {
                settings.DocumentName = "v1";
                settings.Title = "Verus Company API";
                settings.Version = "v1";
            };
        });

    // Omit nulls (e.g. relevanceScore outside relevance searches) for both FastEndpoints and TypedResults.
    builder.Services.ConfigureHttpJsonOptions(options =>
        options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    );

    builder.Services.AddProblemDetails();
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddCors(options =>
        options.AddDefaultPolicy(policy =>
            policy
                .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
                .AllowAnyHeader()
                .AllowAnyMethod()
                .WithExposedHeaders("Location")
        )
    );

    var app = builder.Build();

    app.UseExceptionHandler();
    app.UseSerilogRequestLogging();
    app.UseCors();

    app.UseFastEndpoints(config =>
    {
        config.Endpoints.RoutePrefix = "api";
        config.Endpoints.ShortNames = true;
        config.Versioning.Prefix = "v";
        config.Versioning.PrependToRoute = true;
        config.Serializer.Options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        config.Binding.JsonExceptionTransformer = _ => new FluentValidation.Results.ValidationFailure(
            "body",
            "The request body is not valid JSON."
        );
        config.Errors.UseProblemDetails(problem =>
        {
            problem.IndicateErrorCode = true;
            problem.TitleTransformer = details => ProblemTitles.For(details.Status);
        });
    });

    app.UseSwaggerGen();
    app.MapScalarApiReference(options =>
        options.WithTitle("Verus Company API").WithOpenApiRoutePattern("/swagger/{documentName}/swagger.json")
    );
    app.MapGet("/", () => Results.Redirect("/scalar/v1")).ExcludeFromDescription();

    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    Environment.ExitCode = 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}

/// <summary>Entry point; public so integration tests can use <c>WebApplicationFactory&lt;Program&gt;</c>.</summary>
public partial class Program
{
    protected Program() { }
}
