using System.Globalization;

namespace PaycheckCalculator.Core.ValueObjects;

public readonly record struct Percent(decimal Value)
{
    public static readonly Percent Zero = new(0m);

    public decimal AsFraction => Value / 100m;

    public static Percent FromFraction(decimal fraction) => new(fraction * 100m);

    public override string ToString() => Value.ToString("0.####", CultureInfo.InvariantCulture) + "%";
}
