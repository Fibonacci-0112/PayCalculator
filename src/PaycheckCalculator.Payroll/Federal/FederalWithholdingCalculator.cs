using PaycheckCalculator.Core.Calculations;
using PaycheckCalculator.Core.Jurisdictions;
using PaycheckCalculator.Core.ValueObjects;
using PaycheckCalculator.Core.W4;
using PaycheckCalculator.Payroll.Engine;
using PaycheckCalculator.TaxRules.Federal2026;
using PaycheckCalculator.TaxRules.Model;

namespace PaycheckCalculator.Payroll.Federal;

public sealed record FederalWithholdingContext(
    Money PeriodFederalTaxableWages,
    int AnnualPayPeriods,
    W4Profile W4,
    TaxYear TaxYear,
    RoundingPolicy RoundingPolicy,
    TaxRuleSet FederalRuleSet);

public sealed class FederalWithholdingCalculator
{
    public TaxLineResult Calculate(FederalWithholdingContext ctx, ExplainabilityWriter explain)
    {
        var annualWages = ctx.PeriodFederalTaxableWages * ctx.AnnualPayPeriods;
        var standardDeduction = ctx.W4.MultipleJobsOrSpouseWorks
            ? Money.Zero
            : Money.Usd(FederalRule2026.StandardDeductionForWithholding[ctx.W4.FilingStatus]);

        var adjustedAnnual = annualWages
            + ctx.W4.Step4aOtherIncomeAnnual
            - ctx.W4.Step4bDeductionsAnnual
            - standardDeduction;

        if (adjustedAnnual < Money.Zero) adjustedAnnual = Money.Zero;

        var brackets = ctx.W4.MultipleJobsOrSpouseWorks
            ? FederalRule2026.MultipleJobsBracketsFor(ctx.W4.FilingStatus)
            : FederalRule2026.StandardBracketsFor(ctx.W4.FilingStatus);

        var annualTaxBeforeCredits = BracketMath.Apply(adjustedAnnual.Amount, brackets);
        var annualTaxAfterCredits = Math.Max(0m, annualTaxBeforeCredits - ctx.W4.Step3DependentsCredit.Amount);
        var perPeriod = ctx.RoundingPolicy.Round(annualTaxAfterCredits / ctx.AnnualPayPeriods);
        var withExtra = perPeriod + ctx.W4.Step4cExtraWithholdingPerPeriod.Amount;
        var finalAmount = Money.Usd(withExtra);

        var formulaId = "federal.fit.pub15t.percent";
        var explanation = new ExplainLine(
            LineId: "fed-fit",
            Label: "Federal Income Tax Withholding",
            Amount: finalAmount,
            FormulaId: formulaId,
            FormulaText: "annualizedWages = periodFederalTaxableWages * periodsPerYear; adjusted = annualized + 4a - 4b - standardDeduction; tax = brackets(adjusted); periodTax = (tax - step3Credits) / periodsPerYear + step4cExtra",
            Inputs: new Dictionary<string, string>
            {
                ["periodFederalTaxableWages"] = ctx.PeriodFederalTaxableWages.ToString(),
                ["annualPayPeriods"] = ctx.AnnualPayPeriods.ToString(),
                ["filingStatus"] = ctx.W4.FilingStatus.ToString(),
                ["multipleJobsOrSpouseWorks"] = ctx.W4.MultipleJobsOrSpouseWorks.ToString(),
                ["standardDeduction"] = standardDeduction.ToString(),
                ["step4aOtherIncome"] = ctx.W4.Step4aOtherIncomeAnnual.ToString(),
                ["step4bDeductions"] = ctx.W4.Step4bDeductionsAnnual.ToString(),
                ["step3Credits"] = ctx.W4.Step3DependentsCredit.ToString(),
                ["step4cExtraPerPeriod"] = ctx.W4.Step4cExtraWithholdingPerPeriod.ToString(),
                ["annualTaxBeforeCredits"] = annualTaxBeforeCredits.ToString("0.00"),
                ["annualTaxAfterCredits"] = annualTaxAfterCredits.ToString("0.00")
            },
            RuleSetVersion: ctx.FederalRuleSet.RuleSetVersion,
            TaxYear: ctx.TaxYear,
            JurisdictionCode: "US",
            RoundingMethod: ctx.RoundingPolicy.Name,
            SourceForms: new[] { "IRS Publication 15-T", "Form W-4 (Steps 2, 3, 4a, 4b, 4c)" });
        explain.Add(explanation);

        var authority = new TaxAuthorityRef("federal", "United States", TaxAuthorityType.Federal, "US");
        return new TaxLineResult(
            Authority: authority,
            TaxType: "FederalIncomeTax",
            TaxableWages: ctx.PeriodFederalTaxableWages,
            TaxAmount: finalAmount,
            FormulaId: formulaId,
            SupportLevel: TaxRuleSupportLevel.Verified,
            Explanation: new[] { explanation },
            Warnings: Array.Empty<DiagnosticWarning>());
    }
}
