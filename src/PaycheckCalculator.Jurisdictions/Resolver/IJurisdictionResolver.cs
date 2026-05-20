using PaycheckCalculator.Core.Jurisdictions;
using PaycheckCalculator.Core.ValueObjects;

namespace PaycheckCalculator.Jurisdictions.Resolver;

public sealed record JurisdictionRequest(
    string? HomeStreet,
    string? HomeCity,
    string? HomeStateCode,
    string? HomePostalCode,
    string? WorkStateCode,
    string? RemoteStateCode,
    ResidencyStatus ResidencyStatus,
    TaxYear TaxYear);

public sealed record GeocodeResult(double Latitude, double Longitude, string? Source, JurisdictionConfidence Confidence);

public interface IGeocodingProvider
{
    Task<GeocodeResult?> GeocodeAsync(JurisdictionRequest request, CancellationToken ct);
}

public interface IBoundaryProvider
{
    Task<IReadOnlyList<TaxAuthorityRef>> ResolveAuthoritiesAsync(GeocodeResult point, TaxYear taxYear, CancellationToken ct);
}

public interface IJurisdictionResolver
{
    Task<JurisdictionContext> ResolveAsync(JurisdictionRequest request, CancellationToken ct);
}

public sealed class ManualJurisdictionResolver : IJurisdictionResolver
{
    public Task<JurisdictionContext> ResolveAsync(JurisdictionRequest request, CancellationToken ct)
    {
        var authorities = new List<TaxAuthorityRef>
        {
            new("federal", "United States", TaxAuthorityType.Federal, "US")
        };
        if (!string.IsNullOrWhiteSpace(request.HomeStateCode))
        {
            authorities.Add(new TaxAuthorityRef(
                AuthorityId: $"state-{request.HomeStateCode}",
                DisplayName: request.HomeStateCode!,
                AuthorityType: TaxAuthorityType.StateIncomeTax,
                JurisdictionCode: request.HomeStateCode!));
        }
        return Task.FromResult(new JurisdictionContext(
            HomeAddress: new AddressResolution(
                request.HomeStreet, request.HomeCity, request.HomeStateCode, request.HomePostalCode,
                County: null, Confidence: JurisdictionConfidence.UserOverride, Source: "ManualEntry"),
            WorkAddress: null,
            ResidencyStatus: request.ResidencyStatus,
            ResidentStateCode: request.HomeStateCode ?? "US",
            WorkStateCode: request.WorkStateCode,
            RemoteWorkStateCode: request.RemoteStateCode,
            TaxAuthorities: authorities,
            ReciprocityRules: Array.Empty<ReciprocityRuleRef>(),
            AllocationRules: Array.Empty<AllocationRuleRef>(),
            Confidence: JurisdictionConfidence.UserOverride));
    }
}
