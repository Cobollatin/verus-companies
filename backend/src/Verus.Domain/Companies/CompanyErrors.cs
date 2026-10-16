using ErrorOr;
using Verus.Domain.Common;
using Verus.Domain.Relevance;

namespace Verus.Domain.Companies;

/// <summary>Every expected failure of the Company model, with a stable code and a user-friendly message.</summary>
public static class CompanyErrors
{
    public const string NameField = "name";
    public const string WebsiteField = "websiteUrl";

    public static readonly Error NameRequired = Validation(
        NameField,
        "Company.Name.Required",
        "Company name is required."
    );

    public static readonly Error NameTooShort = Validation(
        NameField,
        "Company.Name.TooShort",
        $"Company name must be at least {CompanyName.MinLength} characters long."
    );

    public static readonly Error NameTooLong = Validation(
        NameField,
        "Company.Name.TooLong",
        $"Company name must be at most {CompanyName.MaxLength} characters long."
    );

    public static readonly Error NameInvalid = Validation(
        NameField,
        "Company.Name.Invalid",
        "Company name must contain letters or digits."
    );

    public static readonly Error WebsiteRequired = Validation(
        WebsiteField,
        "Company.Website.Required",
        "Website URL is required."
    );

    public static readonly Error WebsiteTooLong = Validation(
        WebsiteField,
        "Company.Website.TooLong",
        $"Website URL must be at most {WebsiteUrl.MaxLength} characters long."
    );

    public static readonly Error WebsiteInvalid = Validation(
        WebsiteField,
        "Company.Website.Invalid",
        "Website URL must be a valid absolute URL, e.g. https://www.example.com."
    );

    public static readonly Error WebsiteSchemeNotSupported = Validation(
        WebsiteField,
        "Company.Website.SchemeNotSupported",
        "Website URL must start with http:// or https://."
    );

    public static readonly Error WebsiteUnknownDomain = Validation(
        WebsiteField,
        "Company.Website.UnknownDomain",
        "Website URL must use a registrable domain name with a known top-level domain."
    );

    public static Error NotRelevant(RelevanceAssessment assessment) =>
        Validation(
            WebsiteField,
            "Company.Website.NotRelevant",
            $"The company name does not match the website. {assessment.Reason}"
        );

    public static Error DuplicateDomain(string registrableDomain) =>
        Error.Conflict(
            "Company.Website.Duplicate",
            $"A company with the website domain '{registrableDomain}' is already registered.",
            ErrorMetadata.ForField(WebsiteField)
        );

    public static Error NotFound(CompanyId id) =>
        Error.NotFound("Company.NotFound", $"No company exists with id '{id}'.");

    private static Error Validation(string field, string code, string description) =>
        Error.Validation(code, description, ErrorMetadata.ForField(field));
}
