using ErrorOr;

namespace Verus.Domain.Companies;

/// <summary>A trimmed, non-empty company name of reasonable length.</summary>
public sealed record CompanyName
{
    public const int MinLength = 3;
    public const int MaxLength = 200;

    private CompanyName(string value) => Value = value;

    public string Value { get; }

    public static ErrorOr<CompanyName> Create(string? value)
    {
        // Collapses runs of whitespace; whitespace-only input becomes "".
        var normalized = string.Join(' ', value?.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries) ?? []);

        return normalized switch
        {
            "" => CompanyErrors.NameRequired,
            { Length: < MinLength } => CompanyErrors.NameTooShort,
            { Length: > MaxLength } => CompanyErrors.NameTooLong,
            _ when !normalized.Any(char.IsLetterOrDigit) => CompanyErrors.NameInvalid,
            _ => new CompanyName(normalized),
        };
    }

    public override string ToString() => Value;
}
