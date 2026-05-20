using PaycheckCalculator.Core.Deductions;

namespace PaycheckCalculator.Payroll.Deductions;

public static class DeductionClassifier
{
    public static DeductionTaxTreatment Classify(DeductionType type) => type switch
    {
        DeductionType.Traditional401k => DeductionTaxTreatment.Traditional401k,
        DeductionType.Traditional403b => DeductionTaxTreatment.Traditional401k,
        DeductionType.Traditional457 => DeductionTaxTreatment.Traditional401k,
        DeductionType.Roth401k => DeductionTaxTreatment.Roth401k,
        DeductionType.HsaEmployee => DeductionTaxTreatment.ReducesAll,
        DeductionType.FsaHealthcare => DeductionTaxTreatment.ReducesAll,
        DeductionType.FsaDependentCare => DeductionTaxTreatment.ReducesAll,
        DeductionType.HealthInsuranceCafeteria => DeductionTaxTreatment.ReducesAll,
        DeductionType.DentalInsuranceCafeteria => DeductionTaxTreatment.ReducesAll,
        DeductionType.VisionInsuranceCafeteria => DeductionTaxTreatment.ReducesAll,
        DeductionType.UnionDues => DeductionTaxTreatment.ReducesNone,
        DeductionType.Garnishment => DeductionTaxTreatment.ReducesNone,
        DeductionType.CharitablePayroll => DeductionTaxTreatment.ReducesNone,
        DeductionType.OtherPreTax => DeductionTaxTreatment.ReducesAll,
        DeductionType.OtherPostTax => DeductionTaxTreatment.ReducesNone,
        _ => DeductionTaxTreatment.ReducesNone
    };
}
