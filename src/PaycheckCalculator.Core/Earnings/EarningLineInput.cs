using PaycheckCalculator.Core.ValueObjects;

namespace PaycheckCalculator.Core.Earnings;

public sealed record EarningLineInput(
    EarningType Type,
    Money Amount,
    decimal? Hours = null,
    Money? Rate = null,
    bool IsSupplemental = false,
    TaxTreatmentCode TaxTreatment = TaxTreatmentCode.FullyTaxable,
    string? Note = null);
