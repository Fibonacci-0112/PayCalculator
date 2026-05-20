using PaycheckCalculator.Core.Calculations;
using PaycheckCalculator.Core.Jurisdictions;
using PaycheckCalculator.Core.ValueObjects;
using PaycheckCalculator.Core.W4;
using PaycheckCalculator.Core.Ytd;
using PaycheckCalculator.Payroll.Engine;
using PaycheckCalculator.TaxRules.Federal2026;

namespace PaycheckCalculator.Payroll.Federal;

public sealed record FicaContext(
    Money PeriodSocialSecurityWages,
    Money PeriodMedicareWages,
    YtdSnapshot Ytd,
    W4Profile? W4,
    TaxYear TaxYear,
    RoundingPolicy RoundingPolicy,
    string RuleSetVersion);

public sealed class FicaCalculator
{
    public IReadOnlyList<TaxLineResult> Calculate(FicaContext ctx, ExplainabilityWriter explain,
        ICollection<DiagnosticWarning> warnings)
    {
        var results = new List<TaxLineResult>();
        var authority = new TaxAuthorityRef("fica", "FICA", TaxAuthorityType.Federal, "US");

        var remainingWageBase = Money.Max(Money.Zero,
            Money.Usd(FederalRule2026.SocialSecurityWageBase) - ctx.Ytd.SocialSecurityWages);
        var ssWages = Money.Min(ctx.PeriodSocialSecurityWages, remainingWageBase);
        var ssTax = Money.Usd(ctx.RoundingPolicy.Round(ssWages.Amount * FederalRule2026.SocialSecurityRateEmployee));

        var ssExplain = new ExplainLine(
            LineId: "fed-ss",
            Label: "Social Security Tax",
            Amount: ssTax,
            FormulaId: "federal.fica.ss",
            FormulaText: "ssTax = rate * min(periodSocialSecurityWages, wageBase - ytdSocialSecurityWages)",
            Inputs: new Dictionary<string, string>
            {
                ["periodSocialSecurityWages"] = ctx.PeriodSocialSecurityWages.ToString(),
                ["ytdSocialSecurityWages"] = ctx.Ytd.SocialSecurityWages.ToString(),
                ["remainingWageBase"] = remainingWageBase.ToString(),
                ["rate"] = FederalRule2026.SocialSecurityRateEmployee.ToString("0.####")
            },
            RuleSetVersion: ctx.RuleSetVersion,
            TaxYear: ctx.TaxYear,
            JurisdictionCode: "US",
            RoundingMethod: ctx.RoundingPolicy.Name,
            SourceForms: new[] { "Pub 15-T", "SSA Wage Base" });
        explain.Add(ssExplain);
        results.Add(new TaxLineResult(authority, "SocialSecurity", ssWages, ssTax,
            "federal.fica.ss", TaxRuleSupportLevel.Verified, new[] { ssExplain }, Array.Empty<DiagnosticWarning>()));

        if (remainingWageBase.Amount > 0m && remainingWageBase <= ctx.PeriodSocialSecurityWages * 2m)
        {
            warnings.Add(new DiagnosticWarning(
                WarningCategory.SocialSecurityWageBaseApproaching, WarningSeverity.Info,
                "Year-to-date Social Security wages are approaching the annual wage base."));
        }

        var medicareTax = Money.Usd(ctx.RoundingPolicy.Round(
            ctx.PeriodMedicareWages.Amount * FederalRule2026.MedicareRateEmployee));
        var medicareExplain = new ExplainLine(
            LineId: "fed-medicare",
            Label: "Medicare Tax",
            Amount: medicareTax,
            FormulaId: "federal.fica.medicare",
            FormulaText: "medicareTax = rate * periodMedicareWages",
            Inputs: new Dictionary<string, string>
            {
                ["periodMedicareWages"] = ctx.PeriodMedicareWages.ToString(),
                ["rate"] = FederalRule2026.MedicareRateEmployee.ToString("0.####")
            },
            RuleSetVersion: ctx.RuleSetVersion,
            TaxYear: ctx.TaxYear,
            JurisdictionCode: "US",
            RoundingMethod: ctx.RoundingPolicy.Name,
            SourceForms: new[] { "Pub 15-T" });
        explain.Add(medicareExplain);
        results.Add(new TaxLineResult(authority, "Medicare", ctx.PeriodMedicareWages, medicareTax,
            "federal.fica.medicare", TaxRuleSupportLevel.Verified, new[] { medicareExplain }, Array.Empty<DiagnosticWarning>()));

        var status = ctx.W4?.FilingStatus ?? FilingStatus.Single;
        var threshold = FederalRule2026.AdditionalMedicareThreshold[status];
        var projectedYearMedicareWages = ctx.Ytd.MedicareWages + ctx.PeriodMedicareWages;
        if (projectedYearMedicareWages.Amount > threshold)
        {
            var over = projectedYearMedicareWages.Amount - threshold;
            var portionThisPeriod = Math.Min(ctx.PeriodMedicareWages.Amount, over);
            var addMedTax = Money.Usd(ctx.RoundingPolicy.Round(portionThisPeriod * FederalRule2026.AdditionalMedicareRate));
            var addExplain = new ExplainLine(
                LineId: "fed-add-medicare",
                Label: "Additional Medicare Tax",
                Amount: addMedTax,
                FormulaId: "federal.fica.addl-medicare",
                FormulaText: "addlMedicare = 0.009 * (wages_in_period_above_threshold)",
                Inputs: new Dictionary<string, string>
                {
                    ["projectedYearMedicareWages"] = projectedYearMedicareWages.ToString(),
                    ["threshold"] = threshold.ToString("0.00"),
                    ["wagesInPeriodOverThreshold"] = portionThisPeriod.ToString("0.00")
                },
                RuleSetVersion: ctx.RuleSetVersion,
                TaxYear: ctx.TaxYear,
                JurisdictionCode: "US",
                RoundingMethod: ctx.RoundingPolicy.Name,
                SourceForms: new[] { "Pub 15-T", "Form 8959" });
            explain.Add(addExplain);
            results.Add(new TaxLineResult(authority, "AdditionalMedicare", Money.Usd(portionThisPeriod), addMedTax,
                "federal.fica.addl-medicare", TaxRuleSupportLevel.Verified, new[] { addExplain }, Array.Empty<DiagnosticWarning>()));

            warnings.Add(new DiagnosticWarning(
                WarningCategory.AdditionalMedicareThresholdApproaching, WarningSeverity.Info,
                $"Projected annual Medicare wages exceed the Additional Medicare threshold of {threshold:0} for {status}."));
        }

        return results;
    }
}
