using PaycheckCalculator.Core.ValueObjects;

namespace PaycheckCalculator.Core.Calculations;

public sealed record ExplainLine(
    string LineId,
    string Label,
    Money Amount,
    string FormulaId,
    string FormulaText,
    IReadOnlyDictionary<string, string> Inputs,
    string RuleSetVersion,
    TaxYear TaxYear,
    string JurisdictionCode,
    string RoundingMethod,
    IReadOnlyList<string> SourceForms,
    Money? DifferenceFromPreviousComparableRun = null);
