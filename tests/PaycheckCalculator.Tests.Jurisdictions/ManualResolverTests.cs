using FluentAssertions;
using PaycheckCalculator.Core.Jurisdictions;
using PaycheckCalculator.Core.ValueObjects;
using PaycheckCalculator.Jurisdictions.Resolver;
using Xunit;

namespace PaycheckCalculator.Tests.Jurisdictions;

public class ManualResolverTests
{
    [Fact]
    public async Task Resolver_returns_federal_and_state_authorities()
    {
        var resolver = new ManualJurisdictionResolver();
        var ctx = await resolver.ResolveAsync(
            new JurisdictionRequest(null, null, "CA", null, null, null, ResidencyStatus.FullYearResident, new TaxYear(2026)),
            CancellationToken.None);

        ctx.TaxAuthorities.Should().Contain(a => a.AuthorityType == TaxAuthorityType.Federal);
        ctx.TaxAuthorities.Should().Contain(a => a.AuthorityType == TaxAuthorityType.StateIncomeTax && a.JurisdictionCode == "CA");
        ctx.Confidence.Should().Be(JurisdictionConfidence.UserOverride);
    }
}
