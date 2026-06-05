using PaycheckCalculator.Core.Deductions;
using PaycheckCalculator.Core.Earnings;
using PaycheckCalculator.Core.ValueObjects;
using PaycheckCalculator.Projections.Optimization;

namespace PaycheckCalculator.SharedUi;

/// <summary>
/// Centralized, human-readable display names for the enums surfaced in the UI.
/// The enum identifiers stay machine-friendly (they are used for serialization,
/// golden cases, and the rules engine); these helpers provide the properly
/// spelled and capitalized labels shown to the user.
/// </summary>
public static class EnumDisplay
{
    public static string ToDisplayName(this PayFrequency frequency) => frequency switch
    {
        PayFrequency.Daily => "Daily",
        PayFrequency.Weekly => "Weekly",
        PayFrequency.Weekly53 => "Weekly-53",
        PayFrequency.Biweekly => "Bi-Weekly",
        PayFrequency.Biweekly27 => "Bi-Weekly-27",
        PayFrequency.SemiMonthly => "Semi-Monthly",
        PayFrequency.Monthly => "Monthly",
        PayFrequency.Quarterly => "Quarterly",
        PayFrequency.SemiAnnual => "Semi-Annual",
        PayFrequency.Annual => "Annual",
        _ => frequency.ToString()
    };

    public static string ToDisplayName(this FilingStatus status) => status switch
    {
        FilingStatus.Single => "Single",
        FilingStatus.MarriedFilingJointly => "Married Filing Jointly",
        FilingStatus.MarriedFilingSeparately => "Married Filing Separately",
        FilingStatus.HeadOfHousehold => "Head of Household",
        FilingStatus.QualifyingSurvivingSpouse => "Qualifying Surviving Spouse",
        _ => status.ToString()
    };

    public static string ToDisplayName(this WorkerType worker) => worker switch
    {
        WorkerType.HourlyW2 => "Hourly (W-2)",
        WorkerType.SalariedW2 => "Salaried (W-2)",
        WorkerType.SelfEmployed => "Self-Employed",
        WorkerType.Mixed => "Mixed",
        _ => worker.ToString()
    };

    public static string ToDisplayName(this EarningType type) => type switch
    {
        EarningType.RegularSalary => "Regular Salary",
        EarningType.RegularHourly => "Regular Hourly",
        EarningType.Overtime => "Overtime",
        EarningType.DoubleTime => "Double Time",
        EarningType.ShiftDifferential => "Shift Differential",
        EarningType.Holiday => "Holiday",
        EarningType.PtoPayout => "PTO Payout",
        EarningType.Sick => "Sick",
        EarningType.Tips => "Tips",
        EarningType.ReportedTips => "Reported Tips",
        EarningType.AllocatedTips => "Allocated Tips",
        EarningType.Bonus => "Bonus",
        EarningType.Commission => "Commission",
        EarningType.Reimbursement => "Reimbursement",
        EarningType.TaxableFringeBenefit => "Taxable Fringe Benefit",
        EarningType.GroupTermLifeImputed => "Group-Term Life (Imputed)",
        EarningType.CompanyCarOrMileage => "Company Car or Mileage",
        EarningType.NonCashTaxableBenefit => "Non-Cash Taxable Benefit",
        EarningType.SelfEmployment1099Nec => "Self-Employment (1099-NEC)",
        EarningType.SelfEmployment1099Misc => "Self-Employment (1099-MISC)",
        EarningType.SelfEmployment1099K => "Self-Employment (1099-K)",
        EarningType.SelfEmploymentCashOrCheck => "Self-Employment (Cash or Check)",
        _ => type.ToString()
    };

    public static string ToDisplayName(this TaxTreatmentCode code) => code switch
    {
        TaxTreatmentCode.FullyTaxable => "Fully Taxable",
        TaxTreatmentCode.NonTaxableReimbursement => "Non-Taxable Reimbursement",
        TaxTreatmentCode.SupplementalWages => "Supplemental Wages",
        TaxTreatmentCode.ImputedIncomeNonCash => "Imputed Income (Non-Cash)",
        _ => code.ToString()
    };

    public static string ToDisplayName(this DeductionType type) => type switch
    {
        DeductionType.Traditional401k => "Traditional 401(k)",
        DeductionType.Roth401k => "Roth 401(k)",
        DeductionType.Traditional403b => "Traditional 403(b)",
        DeductionType.Traditional457 => "Traditional 457",
        DeductionType.HsaEmployee => "HSA (Employee)",
        DeductionType.FsaHealthcare => "Healthcare FSA",
        DeductionType.FsaDependentCare => "Dependent Care FSA",
        DeductionType.HealthInsuranceCafeteria => "Health Insurance (Cafeteria)",
        DeductionType.DentalInsuranceCafeteria => "Dental Insurance (Cafeteria)",
        DeductionType.VisionInsuranceCafeteria => "Vision Insurance (Cafeteria)",
        DeductionType.UnionDues => "Union Dues",
        DeductionType.Garnishment => "Garnishment",
        DeductionType.CharitablePayroll => "Charitable Payroll",
        DeductionType.OtherPreTax => "Other Pre-Tax",
        DeductionType.OtherPostTax => "Other Post-Tax",
        _ => type.ToString()
    };

    public static string ToDisplayName(this DeductionAmountType type) => type switch
    {
        DeductionAmountType.FixedPerPeriod => "Fixed Per Period",
        DeductionAmountType.PercentOfGross => "Percent of Gross",
        DeductionAmountType.PercentOfNet => "Percent of Net",
        _ => type.ToString()
    };

    public static string ToDisplayName(this TaxTreatment treatment) => treatment switch
    {
        TaxTreatment.Reduces => "Reduces",
        TaxTreatment.DoesNotReduce => "Does Not Reduce",
        _ => treatment.ToString()
    };

    public static string ToDisplayName(this OptimizationGoal goal) => goal switch
    {
        OptimizationGoal.MaximizeTakeHomePay => "Maximize Take-Home Pay",
        OptimizationGoal.BreakEvenAtTaxTime => "Break Even at Tax Time",
        OptimizationGoal.TargetRefundAmount => "Target Refund Amount",
        OptimizationGoal.AvoidOwingMoreThanThreshold => "Avoid Owing More Than Threshold",
        OptimizationGoal.HitPriorYearSafeHarbor => "Hit Prior-Year Safe Harbor",
        OptimizationGoal.HitCurrentYearSafeHarbor => "Hit Current-Year Safe Harbor",
        OptimizationGoal.IncreaseRetirementSavings => "Increase Retirement Savings",
        OptimizationGoal.IncreaseHsaContribution => "Increase HSA Contribution",
        OptimizationGoal.ReduceOverWithholding => "Reduce Over-Withholding",
        _ => goal.ToString()
    };
}
