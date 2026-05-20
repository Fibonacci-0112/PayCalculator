using PaycheckCalculator.Core.ValueObjects;
using PaycheckCalculator.Core.Ytd;

namespace PaycheckCalculator.Core.Calculations;

public sealed record CalculationResult(
    Guid ScenarioId,
    DateOnly PayDate,
    Money GrossPay,
    Money FederalTaxableWages,
    Money SocialSecurityWages,
    Money MedicareWages,
    Money StateTaxableWages,
    Money LocalTaxableWages,
    IReadOnlyList<TaxLineResult> Taxes,
    IReadOnlyList<DeductionResult> Deductions,
    Money NetPay,
    AnnualProjectionSnapshot Projection,
    YtdDelta YtdDelta,
    YtdSnapshot UpdatedYtd,
    IReadOnlyList<ExplainLine> Explainability,
    IReadOnlyList<DiagnosticWarning> Warnings,
    CalculationAudit Audit)
{
    public Money TotalFederalTax => Taxes
        .Where(t => t.TaxType is "FederalIncomeTax")
        .Aggregate(Money.Zero, (acc, t) => acc + t.TaxAmount);

    public Money TotalFica => Taxes
        .Where(t => t.TaxType is "SocialSecurity" or "Medicare" or "AdditionalMedicare")
        .Aggregate(Money.Zero, (acc, t) => acc + t.TaxAmount);

    public Money TotalStateAndLocal => Taxes
        .Where(t => t.TaxType.StartsWith("State", StringComparison.Ordinal)
                 || t.TaxType.StartsWith("Local", StringComparison.Ordinal))
        .Aggregate(Money.Zero, (acc, t) => acc + t.TaxAmount);

    public Money TotalPreTaxDeductions => Deductions
        .Where(d => d.IsPreTaxForFederal)
        .Aggregate(Money.Zero, (acc, d) => acc + d.Amount);

    public Money TotalPostTaxDeductions => Deductions
        .Where(d => !d.IsPreTaxForFederal)
        .Aggregate(Money.Zero, (acc, d) => acc + d.Amount);
}
