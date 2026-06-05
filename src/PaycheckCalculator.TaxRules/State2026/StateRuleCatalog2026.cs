using System.Diagnostics.CodeAnalysis;
using PaycheckCalculator.Core.ValueObjects;
using PaycheckCalculator.TaxRules.Federal2026;
using PaycheckCalculator.TaxRules.Model;

namespace PaycheckCalculator.TaxRules.State2026;

/// <summary>
/// Flat-rate state income-tax withholding rules used as the 2026 baseline. The marginal rates are the
/// published statutory flat rates; the standard-deduction / personal-exemption figures are projected
/// 2026 estimates pending the official withholding tables, so paycheck results derived from them are
/// surfaced at <c>Estimated</c> support level. Additional states (including graduated-bracket states)
/// can be added here as data without any engine changes.
/// </summary>
public static class StateRuleCatalog2026
{
    private static readonly DateOnly EffectiveFrom = new(2026, 1, 1);
    private static readonly DateOnly EffectiveTo = new(2026, 12, 31);

    /// <summary>Colorado: 4.40% flat. Colorado taxable income derives from federal taxable income, so
    /// the withholding allowance is approximated with the federal standard deduction by filing status.</summary>
    public static readonly StateWithholdingRule Colorado = new(
        StateCode: "CO",
        DisplayName: "Colorado",
        RuleSetId: "state-co-2026",
        RuleSetVersion: "state-co-2026-v2026.draft",
        SourceDocumentName: "Colorado DR 1098 Withholding Worksheet (projected 2026 estimate)",
        SourceRevision: "2026.draft",
        EffectiveFrom: EffectiveFrom,
        EffectiveTo: EffectiveTo,
        AnnualStandardDeduction: FederalRule2026.StandardDeductionForWithholding,
        Brackets: new[] { new TaxBracket(0m, null, 0m, 0.0440m) });

    /// <summary>Illinois: 4.95% flat, less a flat personal-exemption allowance (estimated at $2,775/year).</summary>
    public static readonly StateWithholdingRule Illinois = new(
        StateCode: "IL",
        DisplayName: "Illinois",
        RuleSetId: "state-il-2026",
        RuleSetVersion: "state-il-2026-v2026.draft",
        SourceDocumentName: "Illinois Booklet IL-700-T (projected 2026 estimate)",
        SourceRevision: "2026.draft",
        EffectiveFrom: EffectiveFrom,
        EffectiveTo: EffectiveTo,
        AnnualStandardDeduction: AllStatuses(2_775m),
        Brackets: new[] { new TaxBracket(0m, null, 0m, 0.0495m) });

    /// <summary>Pennsylvania: 3.07% flat on compensation, with no standard deduction or exemptions.</summary>
    public static readonly StateWithholdingRule Pennsylvania = new(
        StateCode: "PA",
        DisplayName: "Pennsylvania",
        RuleSetId: "state-pa-2026",
        RuleSetVersion: "state-pa-2026-v2026.draft",
        SourceDocumentName: "Pennsylvania Personal Income Tax Employer Withholding (projected 2026 estimate)",
        SourceRevision: "2026.draft",
        EffectiveFrom: EffectiveFrom,
        EffectiveTo: EffectiveTo,
        AnnualStandardDeduction: new Dictionary<FilingStatus, decimal>(),
        Brackets: new[] { new TaxBracket(0m, null, 0m, 0.0307m) });

    /// <summary>Seeded state rules keyed by two-letter state code (case-insensitive).</summary>
    public static readonly IReadOnlyDictionary<string, StateWithholdingRule> Rules =
        new Dictionary<string, StateWithholdingRule>(StringComparer.OrdinalIgnoreCase)
        {
            [Colorado.StateCode] = Colorado,
            [Illinois.StateCode] = Illinois,
            [Pennsylvania.StateCode] = Pennsylvania
        };

    /// <summary>Looks up the withholding rule for a state code, returning false when none is installed.</summary>
    public static bool TryGet(string stateCode, [MaybeNullWhen(false)] out StateWithholdingRule rule) =>
        Rules.TryGetValue(stateCode, out rule);

    private static IReadOnlyDictionary<FilingStatus, decimal> AllStatuses(decimal amount) =>
        Enum.GetValues<FilingStatus>().ToDictionary(status => status, _ => amount);
}
