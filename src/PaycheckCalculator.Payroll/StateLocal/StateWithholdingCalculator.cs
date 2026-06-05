using PaycheckCalculator.Core.Calculations;
using PaycheckCalculator.Core.Jurisdictions;
using PaycheckCalculator.Core.ValueObjects;
using PaycheckCalculator.Payroll.Engine;
using PaycheckCalculator.TaxRules.Model;

namespace PaycheckCalculator.Payroll.StateLocal;

public sealed record StateWithholdingContext(
    StateWithholdingRule Rule,
    Money PeriodStateTaxableWages,
    int AnnualPayPeriods,
    FilingStatus FilingStatus,
    TaxYear TaxYear,
    RoundingPolicy RoundingPolicy);

/// <summary>
/// Computes resident-state income-tax withholding from the supplied <see cref="StateWithholdingRule"/>
/// (resolved from the installed rule bundle), annualizing wages, subtracting the state standard
/// deduction, applying the bracket schedule, and de-annualizing. Mirrors
/// <see cref="Federal.FederalWithholdingCalculator"/>.
/// </summary>
public sealed class StateWithholdingCalculator
{
    public TaxLineResult Calculate(StateWithholdingContext ctx, ExplainabilityWriter explain)
    {
        var rule = ctx.Rule;
        var annualWages = ctx.PeriodStateTaxableWages * ctx.AnnualPayPeriods;
        var standardDeduction = Money.Usd(rule.StandardDeductionFor(ctx.FilingStatus));
        var adjustedAnnual = Money.Max(Money.Zero, annualWages - standardDeduction);

        var annualTax = BracketMath.Apply(adjustedAnnual.Amount, rule.BracketsFor(ctx.FilingStatus));
        var perPeriod = ctx.RoundingPolicy.Round(annualTax / ctx.AnnualPayPeriods);
        var finalAmount = Money.Usd(perPeriod);

        var stateKey = rule.StateCode.ToLowerInvariant();
        var formulaId = $"state.{stateKey}.withholding";
        var explanation = new ExplainLine(
            LineId: $"state-{stateKey}-fit",
            Label: $"{rule.DisplayName} income tax withholding",
            Amount: finalAmount,
            FormulaId: formulaId,
            FormulaText: "annualized = periodStateTaxableWages * periodsPerYear; adjusted = max(0, annualized - standardDeduction); tax = brackets(adjusted); periodTax = tax / periodsPerYear",
            Inputs: new Dictionary<string, string>
            {
                ["periodStateTaxableWages"] = ctx.PeriodStateTaxableWages.ToString(),
                ["annualPayPeriods"] = ctx.AnnualPayPeriods.ToString(),
                ["filingStatus"] = ctx.FilingStatus.ToString(),
                ["standardDeduction"] = standardDeduction.ToString(),
                ["annualTax"] = annualTax.ToString("0.00")
            },
            RuleSetVersion: rule.RuleSetVersion,
            TaxYear: ctx.TaxYear,
            JurisdictionCode: rule.StateCode,
            RoundingMethod: ctx.RoundingPolicy.Name,
            SourceForms: new[] { rule.SourceDocumentName });
        explain.Add(explanation);

        var authority = new TaxAuthorityRef(
            AuthorityId: $"state-{rule.StateCode}",
            DisplayName: rule.DisplayName,
            AuthorityType: TaxAuthorityType.StateIncomeTax,
            JurisdictionCode: rule.StateCode);

        return new TaxLineResult(
            Authority: authority,
            TaxType: "StateIncomeTax",
            TaxableWages: ctx.PeriodStateTaxableWages,
            TaxAmount: finalAmount,
            FormulaId: formulaId,
            SupportLevel: TaxRuleSupportLevel.Estimated,
            Explanation: new[] { explanation },
            Warnings: Array.Empty<DiagnosticWarning>());
    }
}
