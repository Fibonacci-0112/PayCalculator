using System.Globalization;

namespace PaycheckCalculator.Core.ValueObjects;

public readonly record struct Money(decimal Amount, string Currency = "USD") : IComparable<Money>
{
    public static readonly Money Zero = new(0m);

    public static Money Usd(decimal amount) => new(amount);

    public Money Round(RoundingPolicy policy) => this with { Amount = policy.Round(Amount) };

    public int CompareTo(Money other)
    {
        if (!string.Equals(Currency, other.Currency, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Cannot compare {Currency} with {other.Currency}.");
        return Amount.CompareTo(other.Amount);
    }

    public static Money operator +(Money a, Money b) => Combine(a, b, (x, y) => x + y);
    public static Money operator -(Money a, Money b) => Combine(a, b, (x, y) => x - y);
    public static Money operator *(Money a, decimal factor) => a with { Amount = a.Amount * factor };
    public static Money operator *(decimal factor, Money a) => a * factor;
    public static Money operator /(Money a, decimal divisor) => a with { Amount = a.Amount / divisor };
    public static bool operator <(Money a, Money b) => a.CompareTo(b) < 0;
    public static bool operator >(Money a, Money b) => a.CompareTo(b) > 0;
    public static bool operator <=(Money a, Money b) => a.CompareTo(b) <= 0;
    public static bool operator >=(Money a, Money b) => a.CompareTo(b) >= 0;

    public static Money Max(Money a, Money b) => a >= b ? a : b;
    public static Money Min(Money a, Money b) => a <= b ? a : b;

    public override string ToString() => Amount.ToString("0.00", CultureInfo.InvariantCulture) + " " + Currency;

    private static Money Combine(Money a, Money b, Func<decimal, decimal, decimal> op)
    {
        if (!string.Equals(a.Currency, b.Currency, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Cannot combine {a.Currency} with {b.Currency}.");
        return new Money(op(a.Amount, b.Amount), a.Currency);
    }
}
