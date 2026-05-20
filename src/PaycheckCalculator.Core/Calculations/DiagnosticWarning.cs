namespace PaycheckCalculator.Core.Calculations;

public enum WarningCategory
{
    WithholdingTooLow,
    PossibleUnderpaymentPenalty,
    SocialSecurityWageBaseApproaching,
    AdditionalMedicareThresholdApproaching,
    ContributionLimitExceeded,
    CatchUpEligibilityNeeded,
    SupplementalWageMethodApplied,
    AnnualPayPeriodOverrideApplied,
    JurisdictionUnverified,
    ReciprocalAgreementMayApply,
    PartialYearResidencyComplexity,
    UnsupportedRuleFallbackUsed,
    HistoricalFormOnly,
    AddressVerificationNeeded,
    UnusualYtdValue
}

public enum WarningSeverity
{
    Info,
    Warning,
    Critical
}

public sealed record DiagnosticWarning(
    WarningCategory Category,
    WarningSeverity Severity,
    string Message,
    IReadOnlyDictionary<string, string>? Context = null);
