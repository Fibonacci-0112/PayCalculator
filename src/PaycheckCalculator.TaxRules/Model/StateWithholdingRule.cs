using PaycheckCalculator.Core.Deductions;
using PaycheckCalculator.Core.ValueObjects;

namespace PaycheckCalculator.TaxRules.Model;

/// <summary>
/// A state income-tax withholding rule. A flat-rate state is modeled as a single open-ended bracket
/// <c>(0, null, 0, rate)</c>; graduated states supply a full bracket schedule. The same shape feeds
/// the generic state withholding calculator, so adding a state is a data-only change.
/// </summary>
public sealed record StateWithholdingRule(
    string StateCode,
    string DisplayName,
    string RuleSetId,
    string RuleSetVersion,
    string SourceDocumentName,
    string SourceRevision,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    IReadOnlyDictionary<FilingStatus, decimal> AnnualStandardDeduction,
    IReadOnlyList<TaxBracket> Brackets,
    IReadOnlyList<DeductionType> StateTaxablePreTaxDeductions)
{
    /// <summary>Annual standard deduction / personal-exemption allowance for a filing status (0 when none).</summary>
    public decimal StandardDeductionFor(FilingStatus status) =>
        AnnualStandardDeduction.TryGetValue(status, out var amount) ? amount : 0m;

    /// <summary>
    /// Projects this rule onto the canonical <see cref="TaxRuleSet"/> so it can sit alongside the
    /// federal package in the bundle for versioning, audit, and the rules API surface. The standard
    /// deduction is emitted as <see cref="DeductionRule"/> entries so a downloaded package can
    /// reproduce the withholding calculation.
    /// </summary>
    public TaxRuleSet ToRuleSet() => new(
        RuleSetId: RuleSetId,
        TaxYear: new TaxYear(EffectiveFrom.Year),
        JurisdictionCode: StateCode,
        RuleSetVersion: RuleSetVersion,
        EffectiveFrom: EffectiveFrom,
        EffectiveTo: EffectiveTo,
        SourceDocumentName: SourceDocumentName,
        SourceDocumentUri: null,
        SourceRevision: SourceRevision,
        EngineMinVersion: "1.0.0",
        Tables: new[]
        {
            new TaxTable(
                $"withholding-{StateCode.ToLowerInvariant()}",
                $"{DisplayName} state withholding",
                Brackets)
        },
        Formulas: new[]
        {
            new FormulaRule(
                $"state.{StateCode.ToLowerInvariant()}.withholding",
                $"{DisplayName} withholding: marginal brackets applied to annualized wages less the standard deduction",
                "see calculator")
        },
        Deductions: AnnualStandardDeduction
            .OrderBy(entry => entry.Key)
            .Select(entry => new DeductionRule(
                $"standard-deduction-{entry.Key}",
                $"{DisplayName} annual standard deduction ({entry.Key})",
                entry.Value))
            .ToArray(),
        Credits: Array.Empty<CreditRule>(),
        ValidationCases: Array.Empty<ValidationCase>(),
        PackageSignature: null);
}
