using PaycheckCalculator.Core.ValueObjects;

namespace PaycheckCalculator.Core.Calculations;

public sealed record CalculationAudit(
    DateTimeOffset GeneratedAt,
    string EngineVersion,
    IReadOnlyList<string> RuleSetVersions,
    RoundingPolicy RoundingPolicy,
    TaxYear TaxYear,
    string PayFrequency,
    int AnnualPayPeriods);
