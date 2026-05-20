using PaycheckCalculator.Core.ValueObjects;

namespace PaycheckCalculator.Core.Calculations;

public sealed record AnnualProjectionSnapshot(
    TaxYear TaxYear,
    Money ProjectedGrossWages,
    Money ProjectedAdjustedGrossIncome,
    Money ProjectedTaxableIncome,
    Money ProjectedFederalTax,
    Money ProjectedStateTax,
    Money ProjectedFederalCredits,
    Money ProjectedFederalWithholding,
    Money ProjectedEstimatedPayments,
    Money ProjectedRefundOrAmountDue,
    Money RecommendedExtraFederalWithholdingPerPeriod,
    Money RecommendedNextQuarterlyEstimatedPayment,
    ConfidenceLevel Confidence,
    IReadOnlyList<string> AssumptionNotes);
