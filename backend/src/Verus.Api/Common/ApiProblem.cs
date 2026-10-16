using System.Reflection;
using Microsoft.AspNetCore.Http.Metadata;

namespace Verus.Api.Common;

/// <summary>
/// Typed result that writes a FastEndpoints problem-details body with the RFC 9457 media type
/// (<c>application/problem+json</c>), matching the errors FastEndpoints emits on its own.
/// </summary>
public sealed class ApiProblem(FastEndpoints.ProblemDetails details)
    : IResult,
        IStatusCodeHttpResult,
        IValueHttpResult<FastEndpoints.ProblemDetails>,
        IEndpointMetadataProvider
{
    public const string ContentType = "application/problem+json";

    public int? StatusCode => details.Status;

    public FastEndpoints.ProblemDetails Value => details;

    public Task ExecuteAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        httpContext.Response.StatusCode = details.Status;
        return httpContext.Response.WriteAsJsonAsync(
            details,
            options: null,
            contentType: ContentType,
            cancellationToken: httpContext.RequestAborted
        );
    }

    public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Metadata.Add(
            new ProducesResponseTypeMetadata(
                StatusCodes.Status400BadRequest,
                typeof(FastEndpoints.ProblemDetails),
                [ContentType]
            )
        );
    }
}
