using ErrorOr;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Verus.Api.Common;

/// <summary>Maps a use-case result to HTTP: the success value, or problem details for its errors.</summary>
internal static class ErrorOrResults
{
    public static Results<Ok<T>, ApiProblem> ToOk<T>(this ErrorOr<T> result, HttpContext context) =>
        result.Match<Results<Ok<T>, ApiProblem>>(value => TypedResults.Ok(value), errors => errors.ToProblem(context));

    public static Results<Created<T>, ApiProblem> ToCreated<T>(
        this ErrorOr<T> result,
        HttpContext context,
        Func<T, string> location
    ) =>
        result.Match<Results<Created<T>, ApiProblem>>(
            value => TypedResults.Created(location(value), value),
            errors => errors.ToProblem(context)
        );
}
