namespace PaycheckCalculator.Core.ValueObjects;

public enum FilingStatus
{
    Single,
    MarriedFilingJointly,
    MarriedFilingSeparately,
    HeadOfHousehold,
    QualifyingSurvivingSpouse
}

public enum WorkerType
{
    HourlyW2,
    SalariedW2,
    SelfEmployed,
    Mixed
}

public enum ResidencyStatus
{
    FullYearResident,
    PartYearResident,
    Nonresident,
    Remote
}

public enum ConfidenceLevel
{
    High,
    Medium,
    Low
}

public enum JurisdictionConfidence
{
    Verified,
    Estimated,
    UserOverride,
    Unverified
}
