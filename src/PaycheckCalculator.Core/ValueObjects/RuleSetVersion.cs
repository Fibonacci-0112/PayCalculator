namespace PaycheckCalculator.Core.ValueObjects;

public readonly record struct TaxYear(int Year)
{
    public override string ToString() => Year.ToString();
    public static implicit operator int(TaxYear year) => year.Year;
    public static implicit operator TaxYear(int year) => new(year);
}

public readonly record struct JurisdictionCode(string Code)
{
    public static readonly JurisdictionCode UnitedStates = new("US");
    public override string ToString() => Code;
}

public readonly record struct RuleSetVersion(string Value)
{
    public override string ToString() => Value;
}
