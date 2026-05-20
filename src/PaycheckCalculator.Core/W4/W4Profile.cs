using PaycheckCalculator.Core.ValueObjects;

namespace PaycheckCalculator.Core.W4;

public sealed record W4Profile(
    FilingStatus FilingStatus,
    bool MultipleJobsOrSpouseWorks,
    Money Step3DependentsCredit,
    Money Step4aOtherIncomeAnnual,
    Money Step4bDeductionsAnnual,
    Money Step4cExtraWithholdingPerPeriod,
    bool NonresidentAlien = false)
{
    public static W4Profile Default(FilingStatus status) => new(
        status,
        MultipleJobsOrSpouseWorks: false,
        Step3DependentsCredit: Money.Zero,
        Step4aOtherIncomeAnnual: Money.Zero,
        Step4bDeductionsAnnual: Money.Zero,
        Step4cExtraWithholdingPerPeriod: Money.Zero);
}
