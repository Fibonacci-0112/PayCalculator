using PaycheckCalculator.Core.ValueObjects;

namespace PaycheckCalculator.Core.Deductions;

public enum DeductionType
{
    Traditional401k,
    Roth401k,
    Traditional403b,
    Traditional457,
    HsaEmployee,
    FsaHealthcare,
    FsaDependentCare,
    HealthInsuranceCafeteria,
    DentalInsuranceCafeteria,
    VisionInsuranceCafeteria,
    UnionDues,
    Garnishment,
    CharitablePayroll,
    OtherPreTax,
    OtherPostTax
}

public enum DeductionAmountType
{
    FixedPerPeriod,
    PercentOfGross,
    PercentOfNet
}

public enum TaxTreatment
{
    Reduces,
    DoesNotReduce
}

public sealed record DeductionTaxTreatment(
    TaxTreatment FederalIncomeTax,
    TaxTreatment SocialSecurity,
    TaxTreatment Medicare,
    TaxTreatment State,
    TaxTreatment Local)
{
    public static DeductionTaxTreatment ReducesAll => new(
        TaxTreatment.Reduces, TaxTreatment.Reduces, TaxTreatment.Reduces,
        TaxTreatment.Reduces, TaxTreatment.Reduces);

    public static DeductionTaxTreatment ReducesNone => new(
        TaxTreatment.DoesNotReduce, TaxTreatment.DoesNotReduce, TaxTreatment.DoesNotReduce,
        TaxTreatment.DoesNotReduce, TaxTreatment.DoesNotReduce);

    public static DeductionTaxTreatment Traditional401k => new(
        TaxTreatment.Reduces, TaxTreatment.DoesNotReduce, TaxTreatment.DoesNotReduce,
        TaxTreatment.Reduces, TaxTreatment.Reduces);

    public static DeductionTaxTreatment Roth401k => ReducesNone;
}

public sealed record DeductionInput(
    DeductionType Type,
    DeductionAmountType AmountType,
    decimal Amount,
    Money? AnnualLimit = null,
    DeductionTaxTreatment? OverrideTreatment = null,
    string? Label = null);
