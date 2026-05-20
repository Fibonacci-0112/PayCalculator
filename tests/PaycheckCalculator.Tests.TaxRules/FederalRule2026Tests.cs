using FluentAssertions;
using PaycheckCalculator.Core.ValueObjects;
using PaycheckCalculator.TaxRules.Federal2026;
using PaycheckCalculator.TaxRules.Registry;
using Xunit;

namespace PaycheckCalculator.Tests.TaxRules;

public class FederalRule2026Tests
{
    [Fact]
    public void Registry_returns_2026_bundle()
    {
        var registry = new InMemoryRulePackageRegistry();
        var bundle = registry.GetBundle(new TaxYear(2026));
        bundle.Federal.RuleSetVersion.Should().Be(FederalRule2026.Version);
        bundle.Federal.JurisdictionCode.Should().Be("US");
    }

    [Fact]
    public void Registry_throws_for_year_without_rules_installed()
    {
        var registry = new InMemoryRulePackageRegistry();
        var act = () => registry.GetBundle(new TaxYear(2024));
        act.Should().Throw<InvalidOperationException>().WithMessage("*2024*");
    }

    [Theory]
    [InlineData(FilingStatus.Single, 15_750)]
    [InlineData(FilingStatus.HeadOfHousehold, 23_625)]
    [InlineData(FilingStatus.MarriedFilingJointly, 31_500)]
    public void Standard_deductions_match_published_2026_amounts(FilingStatus status, decimal expected)
    {
        FederalRule2026.StandardDeductionForWithholding[status].Should().Be(expected);
    }

    [Fact]
    public void Social_security_wage_base_is_184500_for_2026()
    {
        FederalRule2026.SocialSecurityWageBase.Should().Be(184_500m);
    }

    [Fact]
    public void Multiple_jobs_brackets_have_halved_floors()
    {
        var single = FederalRule2026.StandardBracketsFor(FilingStatus.Single);
        var multi = FederalRule2026.MultipleJobsBracketsFor(FilingStatus.Single);
        multi.Should().HaveCount(single.Count);
        for (var i = 0; i < single.Count; i++)
        {
            multi[i].Floor.Should().Be(single[i].Floor / 2m);
        }
    }
}
