using System.Diagnostics.CodeAnalysis;
using PaycheckCalculator.Core.Deductions;
using PaycheckCalculator.Core.ValueObjects;
using PaycheckCalculator.TaxRules.Federal2026;
using PaycheckCalculator.TaxRules.Model;

namespace PaycheckCalculator.TaxRules.State2026;

/// <summary>
/// State income-tax withholding rules used as the 2026 baseline, covering both flat-rate states (CO, IL,
/// PA — a single open-ended bracket) and graduated-bracket states (VA, whose schedule is shared across
/// filing statuses, and NY, whose schedule varies by filing status). The marginal rates are the published
/// statutory schedules; the standard-deduction / personal-exemption figures are projected 2026 estimates
/// pending the official withholding tables, so paycheck results derived from them are surfaced at
/// <c>Estimated</c> support level. Additional states can be added here as data without any engine changes.
/// </summary>
public static class StateRuleCatalog2026
{
    private static readonly DateOnly EffectiveFrom = new(2026, 1, 1);
    private static readonly DateOnly EffectiveTo = new(2026, 12, 31);

    /// <summary>Elective retirement deferrals that conform to federal pre-tax treatment in most states.</summary>
    private static readonly IReadOnlyList<DeductionType> RetirementDeferrals = new[]
    {
        DeductionType.Traditional401k,
        DeductionType.Traditional403b,
        DeductionType.Traditional457
    };

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
        Brackets: new[] { new TaxBracket(0m, null, 0m, 0.0440m) },
        StateTaxablePreTaxDeductions: Array.Empty<DeductionType>());

    /// <summary>Illinois: 4.95% flat, less the 2026 personal-exemption allowance of $2,925.</summary>
    public static readonly StateWithholdingRule Illinois = new(
        StateCode: "IL",
        DisplayName: "Illinois",
        RuleSetId: "state-il-2026",
        RuleSetVersion: "state-il-2026-v2026.draft",
        SourceDocumentName: "Illinois Booklet IL-700-T (2026)",
        SourceRevision: "2026",
        EffectiveFrom: EffectiveFrom,
        EffectiveTo: EffectiveTo,
        AnnualStandardDeduction: AllStatuses(2_925m),
        Brackets: new[] { new TaxBracket(0m, null, 0m, 0.0495m) },
        StateTaxablePreTaxDeductions: Array.Empty<DeductionType>());

    /// <summary>Pennsylvania: 3.07% flat on compensation, with no standard deduction or exemptions.
    /// Pennsylvania taxes elective 401(k)/403(b)/457 deferrals, so those remain in the taxable base.</summary>
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
        Brackets: new[] { new TaxBracket(0m, null, 0m, 0.0307m) },
        StateTaxablePreTaxDeductions: RetirementDeferrals);

    /// <summary>Virginia: graduated 2%–5.75% schedule that is identical for every filing status, so it is
    /// expressed as a single shared bracket table. The standard deduction varies by filing status.</summary>
    public static readonly StateWithholdingRule Virginia = new(
        StateCode: "VA",
        DisplayName: "Virginia",
        RuleSetId: "state-va-2026",
        RuleSetVersion: "state-va-2026-v2026.draft",
        SourceDocumentName: "Virginia Income Tax Withholding Formula (projected 2026 estimate)",
        SourceRevision: "2026.draft",
        EffectiveFrom: EffectiveFrom,
        EffectiveTo: EffectiveTo,
        AnnualStandardDeduction: new Dictionary<FilingStatus, decimal>
        {
            [FilingStatus.Single] = 8_000m,
            [FilingStatus.HeadOfHousehold] = 8_000m,
            [FilingStatus.MarriedFilingSeparately] = 8_000m,
            [FilingStatus.MarriedFilingJointly] = 16_000m,
            [FilingStatus.QualifyingSurvivingSpouse] = 16_000m
        },
        Brackets: new[]
        {
            new TaxBracket(0m,       3_000m,  0m,   0.02m),
            new TaxBracket(3_000m,   5_000m,  60m,  0.03m),
            new TaxBracket(5_000m,   17_000m, 120m, 0.05m),
            new TaxBracket(17_000m,  null,    720m, 0.0575m)
        },
        StateTaxablePreTaxDeductions: Array.Empty<DeductionType>());

    /// <summary>New York graduated schedule for single, head-of-household, and married-filing-separately filers.</summary>
    private static readonly IReadOnlyList<TaxBracket> NewYorkSingleBrackets = new[]
    {
        new TaxBracket(0m,           8_500m,      0m,           0.0400m),
        new TaxBracket(8_500m,       11_700m,     340m,         0.0450m),
        new TaxBracket(11_700m,      13_900m,     484m,         0.0525m),
        new TaxBracket(13_900m,      80_650m,     599.50m,      0.0550m),
        new TaxBracket(80_650m,      215_400m,    4_270.75m,    0.0600m),
        new TaxBracket(215_400m,     1_077_550m,  12_355.75m,   0.0685m),
        new TaxBracket(1_077_550m,   5_000_000m,  71_413.025m,  0.0965m),
        new TaxBracket(5_000_000m,   25_000_000m, 449_929.45m,  0.1030m),
        new TaxBracket(25_000_000m,  null,        2_509_929.45m, 0.1090m)
    };

    /// <summary>New York graduated schedule for married-filing-jointly and qualifying-surviving-spouse filers.</summary>
    private static readonly IReadOnlyList<TaxBracket> NewYorkMarriedBrackets = new[]
    {
        new TaxBracket(0m,           17_150m,     0m,            0.0400m),
        new TaxBracket(17_150m,      23_600m,     686m,          0.0450m),
        new TaxBracket(23_600m,      27_900m,     976.25m,       0.0525m),
        new TaxBracket(27_900m,      161_550m,    1_202m,        0.0550m),
        new TaxBracket(161_550m,     323_200m,    8_552.75m,     0.0600m),
        new TaxBracket(323_200m,     2_155_350m,  18_251.75m,    0.0685m),
        new TaxBracket(2_155_350m,   5_000_000m,  143_754.025m,  0.0965m),
        new TaxBracket(5_000_000m,   25_000_000m, 418_262.75m,   0.1030m),
        new TaxBracket(25_000_000m,  null,        2_478_262.75m, 0.1090m)
    };

    /// <summary>New York: graduated 4%–10.9% schedule whose brackets differ for married filers. The single
    /// schedule is the default (also used for head-of-household and married-filing-separately withholding,
    /// per the state's withholding tables); married-filing-jointly and qualifying-surviving-spouse use the
    /// married schedule supplied through <see cref="StateWithholdingRule.BracketsByStatus"/>.</summary>
    public static readonly StateWithholdingRule NewYork = new(
        StateCode: "NY",
        DisplayName: "New York",
        RuleSetId: "state-ny-2026",
        RuleSetVersion: "state-ny-2026-v2026.draft",
        SourceDocumentName: "New York State Publication NYS-50-T-NYS Exact Calculation Method (projected 2026 estimate)",
        SourceRevision: "2026.draft",
        EffectiveFrom: EffectiveFrom,
        EffectiveTo: EffectiveTo,
        AnnualStandardDeduction: new Dictionary<FilingStatus, decimal>
        {
            [FilingStatus.Single] = 8_000m,
            [FilingStatus.HeadOfHousehold] = 11_200m,
            [FilingStatus.MarriedFilingSeparately] = 8_000m,
            [FilingStatus.MarriedFilingJointly] = 16_050m,
            [FilingStatus.QualifyingSurvivingSpouse] = 16_050m
        },
        Brackets: NewYorkSingleBrackets,
        StateTaxablePreTaxDeductions: Array.Empty<DeductionType>(),
        BracketsByStatus: new Dictionary<FilingStatus, IReadOnlyList<TaxBracket>>
        {
            [FilingStatus.MarriedFilingJointly] = NewYorkMarriedBrackets,
            [FilingStatus.QualifyingSurvivingSpouse] = NewYorkMarriedBrackets
        });

    /// <summary>Seeded state rules keyed by two-letter state code (case-insensitive).</summary>
    public static readonly IReadOnlyDictionary<string, StateWithholdingRule> Rules =
        new Dictionary<string, StateWithholdingRule>(StringComparer.OrdinalIgnoreCase)
        {
            [Colorado.StateCode] = Colorado,
            [Illinois.StateCode] = Illinois,
            [Pennsylvania.StateCode] = Pennsylvania,
            [Virginia.StateCode] = Virginia,
            [NewYork.StateCode] = NewYork
        };

    /// <summary>Looks up the withholding rule for a state code, returning false when none is installed.</summary>
    public static bool TryGet(string stateCode, [MaybeNullWhen(false)] out StateWithholdingRule rule) =>
        Rules.TryGetValue(stateCode, out rule);

    private static IReadOnlyDictionary<FilingStatus, decimal> AllStatuses(decimal amount) =>
        Enum.GetValues<FilingStatus>().ToDictionary(status => status, _ => amount);
}
