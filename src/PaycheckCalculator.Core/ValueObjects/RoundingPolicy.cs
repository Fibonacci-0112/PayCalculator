namespace PaycheckCalculator.Core.ValueObjects;

public sealed record RoundingPolicy(int Decimals, MidpointRounding Mode, string Name)
{
    public static readonly RoundingPolicy CurrencyHalfAwayFromZeroToCent =
        new(2, MidpointRounding.AwayFromZero, "CurrencyRoundHalfAwayFromZeroToCent");

    public static readonly RoundingPolicy CurrencyHalfToEvenToCent =
        new(2, MidpointRounding.ToEven, "CurrencyRoundHalfToEvenToCent");

    public decimal Round(decimal value) => Math.Round(value, Decimals, Mode);
}
