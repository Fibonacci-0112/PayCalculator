using PaycheckCalculator.Core.Calculations;
using PaycheckCalculator.Core.Jurisdictions;
using PaycheckCalculator.Core.ValueObjects;
using PaycheckCalculator.Payroll.Engine;
using PaycheckCalculator.TaxRules.Federal2026;

namespace PaycheckCalculator.Payroll.Federal;

public sealed record SupplementalWageContext(
    Money SupplementalWagesThisPeriod,
    Money YtdSupplementalWages,
    TaxYear TaxYear,
    RoundingPolicy RoundingPolicy,
    string RuleSetVersion);

public sealed class SupplementalWageCalculator
{
    public TaxLineResult? CalculateFlatSupplemental(
        SupplementalWageContext ctx,
        ExplainabilityWriter explain,
        ICollection<DiagnosticWarning> warnings)
    {
        if (ctx.SupplementalWagesThisPeriod.Amount <= 0m) return null;

        var threshold = Money.Usd(FederalRule2026.SupplementalMillionairePlusThreshold);
        var availableUnderThreshold = Money.Max(Money.Zero, threshold - ctx.YtdSupplementalWages);
        var portionAtFlat = Money.Min(ctx.SupplementalWagesThisPeriod, availableUnderThreshold);
        var portionAtTopRate = ctx.SupplementalWagesThisPeriod - portionAtFlat;

        var flatTax = Money.Usd(ctx.RoundingPolicy.Round(portionAtFlat.Amount * FederalRule2026.SupplementalFlatRate));
        var topTax = Money.Usd(ctx.RoundingPolicy.Round(portionAtTopRate.Amount * FederalRule2026.SupplementalMillionairePlusRate));
        var total = flatTax + topTax;

        var explainLine = new ExplainLine(
            LineId: "fed-supplemental",
            Label: "Federal Supplemental Withholding",
            Amount: total,
            FormulaId: "federal.supplemental.flat",
            FormulaText: $"flat = {FederalRule2026.SupplementalFlatRate:P2} of supplemental wages under ${threshold.Amount:0}; {FederalRule2026.SupplementalMillionairePlusRate:P2} above",
            Inputs: new Dictionary<string, string>
            {
                ["supplementalWagesThisPeriod"] = ctx.SupplementalWagesThisPeriod.ToString(),
                ["ytdSupplementalWages"] = ctx.YtdSupplementalWages.ToString(),
                ["portionAtFlat"] = portionAtFlat.ToString(),
                ["portionAtTopRate"] = portionAtTopRate.ToString()
            },
            RuleSetVersion: ctx.RuleSetVersion,
            TaxYear: ctx.TaxYear,
            JurisdictionCode: "US",
            RoundingMethod: ctx.RoundingPolicy.Name,
            SourceForms: new[] { "Pub 15 (Supplemental Wages)" });
        explain.Add(explainLine);

        warnings.Add(new DiagnosticWarning(
            WarningCategory.SupplementalWageMethodApplied, WarningSeverity.Info,
            "Flat-rate supplemental wage method applied to supplemental earnings."));

        var authority = new TaxAuthorityRef("federal", "United States", TaxAuthorityType.Federal, "US");
        return new TaxLineResult(
            authority,
            "FederalSupplemental",
            ctx.SupplementalWagesThisPeriod,
            total,
            "federal.supplemental.flat",
            TaxRuleSupportLevel.Verified,
            new[] { explainLine },
            Array.Empty<DiagnosticWarning>());
    }
}
