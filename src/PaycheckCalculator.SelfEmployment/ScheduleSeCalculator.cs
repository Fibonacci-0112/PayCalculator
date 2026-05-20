using PaycheckCalculator.Core.ValueObjects;
using PaycheckCalculator.TaxRules.Federal2026;

namespace PaycheckCalculator.SelfEmployment;

public sealed record ScheduleCInput(Money GrossBusinessIncome, Money BusinessExpenses);

public sealed record SelfEmploymentResult(
    Money NetProfit,
    Money SeTaxBase,
    Money SocialSecurityPortion,
    Money MedicarePortion,
    Money TotalSeTax,
    Money HalfSeTaxAdjustment,
    Money QbiEstimate);

public sealed class ScheduleSeCalculator
{
    private const decimal SeTaxBaseRate = 0.9235m;
    private const decimal QbiRate = 0.20m;

    public SelfEmploymentResult Calculate(ScheduleCInput schedC, RoundingPolicy rounding)
    {
        var netProfit = Money.Max(Money.Zero, schedC.GrossBusinessIncome - schedC.BusinessExpenses);
        var seBase = Money.Usd(rounding.Round(netProfit.Amount * SeTaxBaseRate));
        var ssPortion = Money.Usd(rounding.Round(
            Math.Min(seBase.Amount, FederalRule2026.SocialSecurityWageBase)
            * (FederalRule2026.SocialSecurityRateEmployee * 2m)));
        var medicarePortion = Money.Usd(rounding.Round(
            seBase.Amount * (FederalRule2026.MedicareRateEmployee * 2m)));
        var totalSe = ssPortion + medicarePortion;
        var halfSe = Money.Usd(rounding.Round(totalSe.Amount / 2m));
        var qbi = Money.Usd(rounding.Round((netProfit - halfSe).Amount * QbiRate));
        return new SelfEmploymentResult(netProfit, seBase, ssPortion, medicarePortion, totalSe, halfSe, qbi);
    }
}
