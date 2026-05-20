using PaycheckCalculator.Core.ValueObjects;

namespace PaycheckCalculator.Core.Household;

public sealed record HouseholdTaxProfile(
    FilingStatus FilingStatus,
    int DependentChildrenUnder17,
    int OtherDependents,
    Money EstimatedItemizedDeductionsAnnual,
    Money PriorYearTotalTaxLiability,
    Money PriorYearFederalWithholding,
    Money OtherHouseholdWagesAnnual,
    Money OtherTaxableInterestAnnual,
    Money OtherDividendsAnnual,
    Money OtherCapitalGainsAnnual,
    Money RetirementIncomeAnnual,
    Money UnemploymentIncomeAnnual,
    Money EstimatedPaymentsMadeYtd)
{
    public static HouseholdTaxProfile Default(FilingStatus filingStatus) => new(
        filingStatus, 0, 0, Money.Zero, Money.Zero, Money.Zero, Money.Zero,
        Money.Zero, Money.Zero, Money.Zero, Money.Zero, Money.Zero, Money.Zero);
}
