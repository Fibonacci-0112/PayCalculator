using PaycheckCalculator.Core.Jurisdictions;
using PaycheckCalculator.Core.ValueObjects;

namespace PaycheckCalculator.Core.Calculations;

public enum TaxRuleSupportLevel
{
    Verified,
    Estimated,
    Manual,
    Unsupported
}

public sealed record TaxLineResult(
    TaxAuthorityRef Authority,
    string TaxType,
    Money TaxableWages,
    Money TaxAmount,
    string FormulaId,
    TaxRuleSupportLevel SupportLevel,
    IReadOnlyList<ExplainLine> Explanation,
    IReadOnlyList<DiagnosticWarning> Warnings);
