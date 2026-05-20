using PaycheckCalculator.Core.ValueObjects;
using PaycheckCalculator.TaxRules.Model;

namespace PaycheckCalculator.TaxRules.Federal2026;

/// <summary>
/// Federal withholding and FICA constants and brackets used as the 2026 baseline rule package.
/// Values here are based on inflation-adjusted projections of the IRS Publication 15-T Percentage Method
/// tables and the Social Security Administration wage base. They are encoded as a rule package so the
/// real, published 2026 tables can replace them through the rule-update workflow without app deploys.
/// </summary>
public static class FederalRule2026
{
    public const string Version = "federal-2026-pub15t-v2026.01.01";
    public const string EngineMinVersion = "1.0.0";

    public const decimal SocialSecurityRateEmployee = 0.062m;
    public const decimal SocialSecurityWageBase = 184_500m;
    public const decimal MedicareRateEmployee = 0.0145m;
    public const decimal AdditionalMedicareRate = 0.009m;
    public const decimal SupplementalFlatRate = 0.22m;
    public const decimal SupplementalMillionairePlusRate = 0.37m;
    public const decimal SupplementalMillionairePlusThreshold = 1_000_000m;

    public static readonly Dictionary<FilingStatus, decimal> AdditionalMedicareThreshold = new()
    {
        [FilingStatus.Single] = 200_000m,
        [FilingStatus.HeadOfHousehold] = 200_000m,
        [FilingStatus.MarriedFilingJointly] = 250_000m,
        [FilingStatus.MarriedFilingSeparately] = 125_000m,
        [FilingStatus.QualifyingSurvivingSpouse] = 250_000m
    };

    /// <summary>
    /// Standard withholding allowance subtracted from annualized wages for employees whose W-4 is
    /// from 2020 or later before the rate table is applied. Pub 15-T Worksheet 1A, Step 1.
    /// </summary>
    public static readonly Dictionary<FilingStatus, decimal> StandardDeductionForWithholding = new()
    {
        [FilingStatus.Single] = 15_750m,
        [FilingStatus.HeadOfHousehold] = 23_625m,
        [FilingStatus.MarriedFilingJointly] = 31_500m,
        [FilingStatus.MarriedFilingSeparately] = 15_750m,
        [FilingStatus.QualifyingSurvivingSpouse] = 31_500m
    };

    public static readonly IReadOnlyList<TaxBracket> SingleStandardBrackets = new[]
    {
        new TaxBracket(0m,         11_925m,    0m,         0.10m),
        new TaxBracket(11_925m,    48_475m,    1_192.50m,  0.12m),
        new TaxBracket(48_475m,    103_350m,   5_578.50m,  0.22m),
        new TaxBracket(103_350m,   197_300m,   17_651m,    0.24m),
        new TaxBracket(197_300m,   250_525m,   40_199m,    0.32m),
        new TaxBracket(250_525m,   626_350m,   57_231m,    0.35m),
        new TaxBracket(626_350m,   null,       188_769.75m,0.37m)
    };

    public static readonly IReadOnlyList<TaxBracket> MfjStandardBrackets = new[]
    {
        new TaxBracket(0m,         23_850m,    0m,         0.10m),
        new TaxBracket(23_850m,    96_950m,    2_385m,     0.12m),
        new TaxBracket(96_950m,    206_700m,   11_157m,    0.22m),
        new TaxBracket(206_700m,   394_600m,   35_302m,    0.24m),
        new TaxBracket(394_600m,   501_050m,   80_398m,    0.32m),
        new TaxBracket(501_050m,   751_600m,   114_462m,   0.35m),
        new TaxBracket(751_600m,   null,       202_154.50m,0.37m)
    };

    public static readonly IReadOnlyList<TaxBracket> HohStandardBrackets = new[]
    {
        new TaxBracket(0m,         17_000m,    0m,         0.10m),
        new TaxBracket(17_000m,    64_850m,    1_700m,     0.12m),
        new TaxBracket(64_850m,    103_350m,   7_442m,     0.22m),
        new TaxBracket(103_350m,   197_300m,   15_912m,    0.24m),
        new TaxBracket(197_300m,   250_500m,   38_460m,    0.32m),
        new TaxBracket(250_500m,   626_350m,   55_484m,    0.35m),
        new TaxBracket(626_350m,   null,       187_031.50m,0.37m)
    };

    public static IReadOnlyList<TaxBracket> StandardBracketsFor(FilingStatus status) => status switch
    {
        FilingStatus.MarriedFilingJointly => MfjStandardBrackets,
        FilingStatus.QualifyingSurvivingSpouse => MfjStandardBrackets,
        FilingStatus.HeadOfHousehold => HohStandardBrackets,
        _ => SingleStandardBrackets
    };

    /// <summary>
    /// When the multiple-jobs / spouse-works box is checked on a 2020+ W-4, the percentage method tables
    /// use a separate "Standard withholding rate schedules" set that effectively halves the standard
    /// deduction. We model that by using the single-filer brackets at half the income thresholds.
    /// </summary>
    public static IReadOnlyList<TaxBracket> MultipleJobsBracketsFor(FilingStatus status)
    {
        var standard = StandardBracketsFor(status);
        // Halve thresholds; tax-at-floor accumulates from the halved brackets so we recompute base tax.
        var brackets = new List<TaxBracket>();
        decimal accBase = 0m;
        decimal prevCeil = 0m;
        foreach (var b in standard)
        {
            decimal floor = b.Floor / 2m;
            decimal? ceil = b.Ceiling.HasValue ? b.Ceiling.Value / 2m : (decimal?)null;
            decimal baseTax = accBase;
            brackets.Add(new TaxBracket(floor, ceil, baseTax, b.MarginalRate));
            if (ceil.HasValue)
            {
                accBase += (ceil.Value - floor) * b.MarginalRate;
                prevCeil = ceil.Value;
            }
        }
        _ = prevCeil; // suppress unused
        return brackets;
    }

    public static TaxRuleSet ToRuleSet() => new(
        RuleSetId: "federal-2026-pub15t",
        TaxYear: new TaxYear(2026),
        JurisdictionCode: "US",
        RuleSetVersion: Version,
        EffectiveFrom: new DateOnly(2026, 1, 1),
        EffectiveTo: new DateOnly(2026, 12, 31),
        SourceDocumentName: "IRS Publication 15-T (Projected 2026 inflation-adjusted figures)",
        SourceDocumentUri: null,
        SourceRevision: "2026.draft",
        EngineMinVersion: EngineMinVersion,
        Tables: new[]
        {
            new TaxTable("withholding-single", "Single standard withholding", SingleStandardBrackets),
            new TaxTable("withholding-mfj", "Married filing jointly standard withholding", MfjStandardBrackets),
            new TaxTable("withholding-hoh", "Head of household standard withholding", HohStandardBrackets)
        },
        Formulas: new[]
        {
            new FormulaRule("federal.fit.pub15t.percent", "Publication 15-T Percentage Method for automated payroll", "see calculator"),
            new FormulaRule("federal.fica.ss", "Social Security tax: rate * min(wages, wage base remaining)", "0.062 * wages"),
            new FormulaRule("federal.fica.medicare", "Medicare tax: rate * wages", "0.0145 * wages"),
            new FormulaRule("federal.fica.addl-medicare", "Additional Medicare tax above filing threshold", "0.009 * wages_over_threshold"),
            new FormulaRule("federal.supplemental.flat", "Flat supplemental wage rate (under $1M cumulative)", "0.22 * supplemental_wages")
        },
        Deductions: Array.Empty<DeductionRule>(),
        Credits: Array.Empty<CreditRule>(),
        ValidationCases: Array.Empty<ValidationCase>(),
        PackageSignature: null);
}
