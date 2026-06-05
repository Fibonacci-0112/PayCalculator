using FluentAssertions;
using PaycheckCalculator.Core.Deductions;
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
    public void Illinois_uses_the_2026_exemption_and_Pennsylvania_taxes_deferrals()
    {
        StateRuleCatalog2026.Illinois.StandardDeductionFor(FilingStatus.Single).Should().Be(2_925m);
        StateRuleCatalog2026.Pennsylvania.StandardDeductionFor(FilingStatus.Single).Should().Be(0m);
        StateRuleCatalog2026.Pennsylvania.StateTaxablePreTaxDeductions.Should().Contain(DeductionType.Traditional401k);
        StateRuleCatalog2026.Illinois.StateTaxablePreTaxDeductions.Should().BeEmpty();
    }

    [Fact]
    public void Exported_state_package_includes_the_standard_deduction()
    {
        // The /v1/rules export uses TaxRuleSet, so the standard deduction must survive the projection.
        var ruleSet = StateRuleCatalog2026.Illinois.ToRuleSet();
        ruleSet.Deductions.Should().NotBeEmpty();
        ruleSet.Deductions.Should().Contain(d => d.AnnualLimit == 2_925m);
    }

    [Fact]
    public void Registry_bundle_includes_seeded_states()
    {
        var registry = new InMemoryRulePackageRegistry();
        var bundle = registry.GetBundle(new TaxYear(2026));

        bundle.States.Should().ContainKeys("CO", "IL", "PA", "VA", "NY");
        bundle.StateWithholding.Should().ContainKeys("CO", "IL", "PA", "VA", "NY");
        bundle.States["IL"].JurisdictionCode.Should().Be("IL");
        bundle.States["IL"].RuleSetVersion.Should().Be(StateRuleCatalog2026.Illinois.RuleSetVersion);
        bundle.AllRuleSetVersions().Should().Contain(StateRuleCatalog2026.Pennsylvania.RuleSetVersion);
    }

    [Fact]
    public void Catalog_contains_seeded_graduated_states()
    {
        StateRuleCatalog2026.Rules.Should().ContainKeys("VA", "NY");

        // Virginia is a four-step graduated schedule topping out at 5.75%.
        StateRuleCatalog2026.Virginia.Brackets.Should().HaveCount(4);
        StateRuleCatalog2026.Virginia.Brackets[^1].MarginalRate.Should().Be(0.0575m);
        StateRuleCatalog2026.Virginia.Brackets[^1].Ceiling.Should().BeNull();

        // New York is a nine-step graduated schedule topping out at 10.9%.
        StateRuleCatalog2026.NewYork.Brackets.Should().HaveCount(9);
        StateRuleCatalog2026.NewYork.Brackets[^1].MarginalRate.Should().Be(0.1090m);
    }

    [Fact]
    public void Virginia_shares_one_schedule_across_filing_statuses()
    {
        var single = StateRuleCatalog2026.Virginia.BracketsFor(FilingStatus.Single);
        var joint = StateRuleCatalog2026.Virginia.BracketsFor(FilingStatus.MarriedFilingJointly);

        // No per-status override, so every status falls back to the same shared schedule.
        ReferenceEquals(single, joint).Should().BeTrue();
        ReferenceEquals(single, StateRuleCatalog2026.Virginia.Brackets).Should().BeTrue();

        // Only the standard deduction differs between single and joint filers.
        StateRuleCatalog2026.Virginia.StandardDeductionFor(FilingStatus.Single).Should().Be(8_000m);
        StateRuleCatalog2026.Virginia.StandardDeductionFor(FilingStatus.MarriedFilingJointly).Should().Be(16_000m);
    }

    [Fact]
    public void NewYork_uses_distinct_schedules_for_single_and_married_filers()
    {
        var single = StateRuleCatalog2026.NewYork.BracketsFor(FilingStatus.Single);
        var joint = StateRuleCatalog2026.NewYork.BracketsFor(FilingStatus.MarriedFilingJointly);

        ReferenceEquals(single, joint).Should().BeFalse();
        single[0].Ceiling.Should().Be(8_500m);
        joint[0].Ceiling.Should().Be(17_150m);

        // Qualifying surviving spouse shares the married schedule; statuses without an override
        // (head of household, married filing separately) fall back to the single schedule.
        ReferenceEquals(StateRuleCatalog2026.NewYork.BracketsFor(FilingStatus.QualifyingSurvivingSpouse), joint).Should().BeTrue();
        ReferenceEquals(StateRuleCatalog2026.NewYork.BracketsFor(FilingStatus.HeadOfHousehold), single).Should().BeTrue();
        ReferenceEquals(StateRuleCatalog2026.NewYork.BracketsFor(FilingStatus.MarriedFilingSeparately), single).Should().BeTrue();
    }

    [Fact]
    public void Exported_NewYork_package_includes_a_table_per_filing_status_schedule()
    {
        // The /v1/rules export must carry the married schedule, not just the default single one.
        var ruleSet = StateRuleCatalog2026.NewYork.ToRuleSet();

        ruleSet.Tables.Should().Contain(t => t.TableId == "withholding-ny");
        ruleSet.Tables.Should().Contain(t => t.TableId == "withholding-ny-marriedfilingjointly");
        ruleSet.Tables.Should().Contain(t => t.TableId == "withholding-ny-qualifyingsurvivingspouse");
    }
}
