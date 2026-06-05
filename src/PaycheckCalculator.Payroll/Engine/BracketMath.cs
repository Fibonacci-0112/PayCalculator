using PaycheckCalculator.TaxRules.Model;

namespace PaycheckCalculator.Payroll.Engine;

/// <summary>
/// Evaluates a marginal tax-bracket schedule (cumulative base tax at the floor plus the marginal rate
/// on the amount above it). Shared by federal withholding, the annual projection, and state withholding.
/// </summary>
public static class BracketMath
{
    public static decimal Apply(decimal taxableAmount, IReadOnlyList<TaxBracket> brackets)
    {
        foreach (var bracket in brackets)
        {
            if (!bracket.Ceiling.HasValue || taxableAmount <= bracket.Ceiling.Value)
                return bracket.BaseTax + (taxableAmount - bracket.Floor) * bracket.MarginalRate;
        }

        var last = brackets[^1];
        return last.BaseTax + (taxableAmount - last.Floor) * last.MarginalRate;
    }
}
