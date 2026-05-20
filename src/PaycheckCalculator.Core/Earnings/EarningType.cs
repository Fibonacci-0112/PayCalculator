namespace PaycheckCalculator.Core.Earnings;

public enum EarningType
{
    RegularSalary,
    RegularHourly,
    Overtime,
    DoubleTime,
    ShiftDifferential,
    Holiday,
    PtoPayout,
    Sick,
    Tips,
    ReportedTips,
    AllocatedTips,
    Bonus,
    Commission,
    Reimbursement,
    TaxableFringeBenefit,
    GroupTermLifeImputed,
    CompanyCarOrMileage,
    NonCashTaxableBenefit,
    SelfEmployment1099Nec,
    SelfEmployment1099Misc,
    SelfEmployment1099K,
    SelfEmploymentCashOrCheck
}

public enum TaxTreatmentCode
{
    FullyTaxable,
    NonTaxableReimbursement,
    SupplementalWages,
    ImputedIncomeNonCash
}
