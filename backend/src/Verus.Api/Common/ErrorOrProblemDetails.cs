using ErrorOr;
using FluentValidation.Results;
using Verus.Domain.Common;
using ProblemDetails = FastEndpoints.ProblemDetails;

namespace Verus.Api.Common;

/// <summary>
/// Maps application errors to RFC 9457 problem details, using the same shape FastEndpoints emits for
/// binding failures, so clients handle a single error format:
/// <c>{ type, title, status, detail, instance, traceId, errors: [{ name, reason, code }] }</c>.
/// </summary>
internal static class ErrorOrProblemDetails
{
    private const string GeneralErrorsField = "generalErrors";

    public static ApiProblem ToProblem(this IReadOnlyList<Error> errors, HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(errors);
        ArgumentNullException.ThrowIfNull(context);

        var first = errors.Count > 0 ? errors[0] : Error.Unexpected();
        var status = StatusCodeFor(first.Type);

        var failures =
            status >= StatusCodes.Status500InternalServerError
                ? [new ValidationFailure(GeneralErrorsField, "An unexpected error occurred.")]
                : errors
                    .Select(e => new ValidationFailure(e.GetField() ?? GeneralErrorsField, e.Description)
                    {
                        ErrorCode = e.Code,
                    })
                    .ToList();

        return new ApiProblem(
            new ProblemDetails(failures, context.Request.Path, context.TraceIdentifier, status)
            {
                Detail = failures[0].ErrorMessage,
            }
        );
    }

    private static int StatusCodeFor(ErrorType type) =>
        type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status500InternalServerError,
        };
}

internal static class ProblemTitles
{
    public static string For(int status) =>
        status switch
        {
            StatusCodes.Status400BadRequest => "Validation failed",
            StatusCodes.Status404NotFound => "Resource not found",
            StatusCodes.Status409Conflict => "Conflict",
            _ => "An error occurred",
        };
}
