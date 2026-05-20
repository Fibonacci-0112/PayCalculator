using PaycheckCalculator.Core.ValueObjects;

namespace PaycheckCalculator.Core.Jurisdictions;

public enum TaxAuthorityType
{
    Federal,
    StateIncomeTax,
    StateUnemploymentEmployeeTax,
    StateDisabilityInsuranceEmployeeTax,
    PaidFamilyLeaveEmployeeTax,
    CountyIncomeTax,
    CityIncomeTax,
    LocalEarnedIncomeTax,
    SchoolDistrictIncomeTax,
    TransitOrSpecialPayrollTax
}

public sealed record TaxAuthorityRef(
    string AuthorityId,
    string DisplayName,
    TaxAuthorityType AuthorityType,
    string JurisdictionCode);

public sealed record ReciprocityRuleRef(
    string ResidentStateCode,
    string WorkStateCode,
    string Description);

public sealed record AllocationRuleRef(
    string Description,
    decimal? AllocationPercent);

public sealed record AddressResolution(
    string? Street1,
    string? City,
    string? StateCode,
    string? PostalCode,
    string? County,
    JurisdictionConfidence Confidence,
    string? Source);

public sealed record JurisdictionContext(
    AddressResolution? HomeAddress,
    AddressResolution? WorkAddress,
    ResidencyStatus ResidencyStatus,
    string ResidentStateCode,
    string? WorkStateCode,
    string? RemoteWorkStateCode,
    IReadOnlyList<TaxAuthorityRef> TaxAuthorities,
    IReadOnlyList<ReciprocityRuleRef> ReciprocityRules,
    IReadOnlyList<AllocationRuleRef> AllocationRules,
    JurisdictionConfidence Confidence)
{
    public static JurisdictionContext FederalOnly() => new(
        HomeAddress: null,
        WorkAddress: null,
        ResidencyStatus: ResidencyStatus.FullYearResident,
        ResidentStateCode: "US",
        WorkStateCode: null,
        RemoteWorkStateCode: null,
        TaxAuthorities: new[] { new TaxAuthorityRef("federal", "United States", TaxAuthorityType.Federal, "US") },
        ReciprocityRules: Array.Empty<ReciprocityRuleRef>(),
        AllocationRules: Array.Empty<AllocationRuleRef>(),
        Confidence: JurisdictionConfidence.Verified);
}
