namespace Verus.Domain.Companies;

/// <summary>Strongly typed identifier for <see cref="Company"/>. Version 7 GUIDs are time-ordered.</summary>
public readonly record struct CompanyId(Guid Value)
{
    public static CompanyId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();
}
