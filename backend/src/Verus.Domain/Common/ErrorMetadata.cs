using ErrorOr;

namespace Verus.Domain.Common;

/// <summary>Conventions for <see cref="Error.Metadata"/> so outer layers can map errors to input fields.</summary>
public static class ErrorMetadata
{
    public const string FieldKey = "field";

    // ErrorOr requires a concrete Dictionary for metadata.
#pragma warning disable MA0016
    public static Dictionary<string, object> ForField(string field) =>
        new(StringComparer.Ordinal) { [FieldKey] = field };
#pragma warning restore MA0016

    public static string? GetField(this Error error) =>
        error.Metadata is not null && error.Metadata.TryGetValue(FieldKey, out var field) ? field as string : null;
}
