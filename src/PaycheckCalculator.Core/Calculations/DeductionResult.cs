using PaycheckCalculator.Core.Deductions;
using PaycheckCalculator.Core.ValueObjects;

namespace PaycheckCalculator.Core.Calculations;

public sealed record DeductionResult(
    DeductionType Type,
    string Label,
    Money Amount,
    DeductionTaxTreatment Treatment,
    bool IsPreTaxForFederal,
    IReadOnlyList<DiagnosticWarning> Warnings);
