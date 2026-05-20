using PaycheckCalculator.Core.ValueObjects;

namespace PaycheckCalculator.Core.Ytd;

public sealed record YtdSnapshot(
    DateOnly AsOfDate,
    TaxYear TaxYear,
    Money GrossWages,
    Money FederalTaxableWages,
    Money FederalWithholding,
    Money SocialSecurityWages,
    Money SocialSecurityTax,
    Money MedicareWages,
    Money MedicareTax,
    Money AdditionalMedicareTax,
    Money StateWages,
    Money StateWithholding,
    Money LocalWages,
    Money LocalWithholding,
    Money PreTaxDeductions,
    Money PostTaxDeductions,
    int CompletedPayPeriods)
{
    public static YtdSnapshot Empty(TaxYear year) => new(
        DateOnly.FromDateTime(DateTime.UtcNow), year,
        Money.Zero, Money.Zero, Money.Zero,
        Money.Zero, Money.Zero,
        Money.Zero, Money.Zero, Money.Zero,
        Money.Zero, Money.Zero,
        Money.Zero, Money.Zero,
        Money.Zero, Money.Zero, 0);

    public YtdSnapshot Apply(YtdDelta delta) => this with
    {
        GrossWages = GrossWages + delta.GrossWages,
        FederalTaxableWages = FederalTaxableWages + delta.FederalTaxableWages,
        FederalWithholding = FederalWithholding + delta.FederalWithholding,
        SocialSecurityWages = SocialSecurityWages + delta.SocialSecurityWages,
        SocialSecurityTax = SocialSecurityTax + delta.SocialSecurityTax,
        MedicareWages = MedicareWages + delta.MedicareWages,
        MedicareTax = MedicareTax + delta.MedicareTax,
        AdditionalMedicareTax = AdditionalMedicareTax + delta.AdditionalMedicareTax,
        StateWages = StateWages + delta.StateWages,
        StateWithholding = StateWithholding + delta.StateWithholding,
        LocalWages = LocalWages + delta.LocalWages,
        LocalWithholding = LocalWithholding + delta.LocalWithholding,
        PreTaxDeductions = PreTaxDeductions + delta.PreTaxDeductions,
        PostTaxDeductions = PostTaxDeductions + delta.PostTaxDeductions,
        CompletedPayPeriods = CompletedPayPeriods + 1,
        AsOfDate = delta.PayDate
    };
}

public sealed record YtdDelta(
    DateOnly PayDate,
    Money GrossWages,
    Money FederalTaxableWages,
    Money FederalWithholding,
    Money SocialSecurityWages,
    Money SocialSecurityTax,
    Money MedicareWages,
    Money MedicareTax,
    Money AdditionalMedicareTax,
    Money StateWages,
    Money StateWithholding,
    Money LocalWages,
    Money LocalWithholding,
    Money PreTaxDeductions,
    Money PostTaxDeductions);
