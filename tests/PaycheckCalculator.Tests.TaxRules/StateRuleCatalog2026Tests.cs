using FluentAssertions;
using PaycheckCalculator.Core.ValueObjects;
using PaycheckCalculator.TaxRules.Registry;
using PaycheckCalculator.TaxRules.State2026;
using Xunit;

namespace PaycheckCalculator.Tests.TaxRules;

public class StateRuleCatalog2026Tests
{
    [Fact]
    public void Catalog_contains_seeded_flat_rate_states()
    {
        StateRuleCatalog2026.Rules.Should().ContainKeys("CO", "IL", "PA");
        StateRuleCatalog2026.Colorado.Brackets.Should().ContainSingle()
            .Which.MarginalRate.Should().Be(0.0440m);
        StateRuleCatalog2026.Illinois.Brackets[0].MarginalRate.Should().Be(0.0495m);
        StateRuleCatalog2026.Pennsylvania.Brackets[0].MarginalRate.Should().Be(0.0307m);
    }

    [Fact]
    public void TryGet_is_case_insensitive_and_returns_false_for_unknown_states()
    {
        StateRuleCatalog2026.TryGet("il", out _).Should().BeTrue();
        StateRuleCatalog2026.Rules["IL"].StateCode.Should().Be("IL");
        StateRuleCatalog2026.TryGet("ZZ", out _).Should().BeFalse();
    }

    [Fact]
    public void Pennsylvania_has_no_standard_deduction()
    {
        StateRuleCatalog2026.Pennsylvania.StandardDeductionFor(FilingStatus.Single).Should().Be(0m);
    }

    [Fact]
    public void Registry_bundle_includes_seeded_states()
    {
        var registry = new InMemoryRulePackageRegistry();
        var bundle = registry.GetBundle(new TaxYear(2026));

        bundle.States.Should().ContainKeys("CO", "IL", "PA");
        bundle.States["IL"].JurisdictionCode.Should().Be("IL");
        bundle.States["IL"].RuleSetVersion.Should().Be(StateRuleCatalog2026.Illinois.RuleSetVersion);
        bundle.AllRuleSetVersions().Should().Contain(StateRuleCatalog2026.Pennsylvania.RuleSetVersion);
    }
}
