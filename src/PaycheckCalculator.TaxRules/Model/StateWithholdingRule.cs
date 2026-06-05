using PaycheckCalculator.Core.Deductions;
using PaycheckCalculator.Core.ValueObjects;

namespace PaycheckCalculator.TaxRules.Model;

/// <summary>
/// A state income-tax withholding rule. A flat-rate state is modeled as a single open-ended bracket
/// <c>(0, null, 0, rate)</c>; graduated states supply a full bracket schedule. States whose schedule
/// varies by filing status (e.g. New York) supply per-status schedules in <see cref="BracketsByStatus"/>
/// and use <see cref="Brackets"/> as the default for any status not overridden. The same shape feeds
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
    IReadOnlyList<DeductionType> StateTaxablePreTaxDeductions,
    IReadOnlyDictionary<FilingStatus, IReadOnlyList<TaxBracket>>? BracketsByStatus = null)
{
    /// <summary>Annual standard deduction / personal-exemption allowance for a filing status (0 when none).</summary>
    public decimal StandardDeductionFor(FilingStatus status) =>
        AnnualStandardDeduction.TryGetValue(status, out var amount) ? amount : 0m;

    /// <summary>
    /// The withholding bracket schedule for a filing status: the status-specific schedule when one is
    /// supplied (graduated states that differ by status), otherwise the default <see cref="Brackets"/>.
    /// </summary>
    public IReadOnlyList<TaxBracket> BracketsFor(FilingStatus status) =>
        BracketsByStatus is not null && BracketsByStatus.TryGetValue(status, out var brackets)
            ? brackets
            : Brackets;

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
        Tables: BuildWithholdingTables(),
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

    /// <summary>
    /// Emits the default withholding table plus one table per status-specific schedule, so a downloaded
    /// package reproduces a graduated state's per-filing-status brackets, not just the default schedule.
    /// </summary>
    private IReadOnlyList<TaxTable> BuildWithholdingTables()
    {
        var key = StateCode.ToLowerInvariant();
        var tables = new List<TaxTable>
        {
            new($"withholding-{key}", $"{DisplayName} state withholding", Brackets)
        };

        if (BracketsByStatus is not null)
        {
            foreach (var entry in BracketsByStatus.OrderBy(pair => pair.Key))
            {
                tables.Add(new TaxTable(
                    $"withholding-{key}-{entry.Key.ToString().ToLowerInvariant()}",
                    $"{DisplayName} state withholding ({entry.Key})",
                    entry.Value));
            }
        }

        return tables;
    }
}
