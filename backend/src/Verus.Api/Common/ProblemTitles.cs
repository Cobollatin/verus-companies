namespace Verus.Api.Common;

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
