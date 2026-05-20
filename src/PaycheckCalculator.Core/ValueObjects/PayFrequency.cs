namespace PaycheckCalculator.Core.ValueObjects;

public enum PayFrequency
{
    Daily,
    Weekly,
    Weekly53,
    Biweekly,
    Biweekly27,
    SemiMonthly,
    Monthly,
    Quarterly,
    SemiAnnual,
    Annual
}

public static class PayFrequencyExtensions
{
    public static int AnnualPeriods(this PayFrequency frequency) => frequency switch
    {
        PayFrequency.Daily => 260,
        PayFrequency.Weekly => 52,
        PayFrequency.Weekly53 => 53,
        PayFrequency.Biweekly => 26,
        PayFrequency.Biweekly27 => 27,
        PayFrequency.SemiMonthly => 24,
        PayFrequency.Monthly => 12,
        PayFrequency.Quarterly => 4,
        PayFrequency.SemiAnnual => 2,
        PayFrequency.Annual => 1,
        _ => throw new ArgumentOutOfRangeException(nameof(frequency), frequency, "Unknown pay frequency.")
    };

    public static bool IsExtendedPeriod(this PayFrequency frequency) =>
        frequency is PayFrequency.Weekly53 or PayFrequency.Biweekly27;
}
