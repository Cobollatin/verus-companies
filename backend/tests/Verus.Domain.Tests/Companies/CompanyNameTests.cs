using Verus.Domain.Companies;

namespace Verus.Domain.Tests.Companies;

public sealed class CompanyNameTests
{
    [Theory]
    [InlineData("Acme", "Acme")]
    [InlineData("  Acme   Corp  ", "Acme Corp")]
    [InlineData("3M Co", "3M Co")]
    public void Create_WithValidName_NormalizesWhitespace(string input, string expected)
    {
        var result = CompanyName.Create(input);

        result.IsError.ShouldBeFalse();
        result.Value.Value.ShouldBe(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithMissingName_ReturnsRequired(string? input) =>
        CompanyName.Create(input).FirstError.ShouldBe(CompanyErrors.NameRequired);

    [Theory]
    [InlineData("A")]
    [InlineData(" Ab ")]
    public void Create_WithTooShortName_ReturnsTooShort(string input) =>
        CompanyName.Create(input).FirstError.ShouldBe(CompanyErrors.NameTooShort);

    [Fact]
    public void Create_WithTooLongName_ReturnsTooLong() =>
        CompanyName.Create(new string('a', CompanyName.MaxLength + 1)).FirstError.ShouldBe(CompanyErrors.NameTooLong);

    [Fact]
    public void Create_WithOnlySymbols_ReturnsInvalid() =>
        CompanyName.Create("!!!").FirstError.ShouldBe(CompanyErrors.NameInvalid);
}
