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
        if (string.IsNullOrWhiteSpace(value))
        {
            return CompanyErrors.NameRequired;
        }

        var normalized = string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

        if (normalized.Length < MinLength)
        {
            return CompanyErrors.NameTooShort;
        }

        if (normalized.Length > MaxLength)
        {
            return CompanyErrors.NameTooLong;
        }

        if (!normalized.Any(char.IsLetterOrDigit))
        {
            return CompanyErrors.NameInvalid;
        }

        return new CompanyName(normalized);
    }

    public override string ToString() => Value;
}
