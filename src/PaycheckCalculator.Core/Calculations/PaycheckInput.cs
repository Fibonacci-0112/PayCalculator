using PaycheckCalculator.Core.Deductions;
using PaycheckCalculator.Core.Earnings;
using PaycheckCalculator.Core.Household;
using PaycheckCalculator.Core.Jurisdictions;
using PaycheckCalculator.Core.ValueObjects;
using PaycheckCalculator.Core.W4;
using PaycheckCalculator.Core.Ytd;

namespace PaycheckCalculator.Core.Calculations;

public sealed record PaycheckInput(
    Guid ScenarioId,
    TaxYear TaxYear,
    PayFrequency PayFrequency,
    WorkerType WorkerType,
    IReadOnlyList<EarningLineInput> Earnings,
    IReadOnlyList<DeductionInput> Deductions,
    W4Profile? W4,
    HouseholdTaxProfile Household,
    JurisdictionContext Jurisdictions,
    YtdSnapshot Ytd,
    RoundingPolicy RoundingPolicy,
    DateOnly PayDate);
