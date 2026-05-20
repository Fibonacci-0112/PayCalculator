using FluentAssertions;
using PaycheckCalculator.Core.Deductions;
using PaycheckCalculator.Core.Earnings;
using PaycheckCalculator.Core.ValueObjects;
using PaycheckCalculator.Core.W4;
using PaycheckCalculator.Payroll.Engine;
using PaycheckCalculator.SharedUi.Services;
using PaycheckCalculator.TaxRules.Registry;
using Xunit;

namespace PaycheckCalculator.Tests.Payroll;

public class ScenarioEditorStateTests
{
    private static ScenarioEditorState NewState() =>
        new(new PaycheckPipeline(), new InMemoryRulePackageRegistry());

    [Fact]
    public void Adding_a_deduction_invalidates_cached_result_and_raises_changed()
    {
        var state = NewState();
        state.Recalculate();
        state.LatestResult.Should().NotBeNull();
        var raised = 0;
        state.Changed += () => raised++;

        state.AddDeduction(new DeductionInput(DeductionType.Traditional401k, DeductionAmountType.PercentOfGross, 5m));

        raised.Should().Be(1);
        state.LatestResult.Should().BeNull();
    }

    [Fact]
    public void Updating_w4_propagates_filing_status_to_household()
    {
        var state = NewState();

        state.UpdateW4(state.W4 with { FilingStatus = FilingStatus.MarriedFilingJointly });

        state.Household.FilingStatus.Should().Be(FilingStatus.MarriedFilingJointly);
    }

    [Fact]
    public void Updating_household_propagates_filing_status_to_w4()
    {
        var state = NewState();

        state.UpdateHousehold(state.Household with { FilingStatus = FilingStatus.HeadOfHousehold });

        state.W4.FilingStatus.Should().Be(FilingStatus.HeadOfHousehold);
    }

    [Fact]
    public void Recalculate_produces_a_result_with_explainability()
    {
        var state = NewState();

        var result = state.Recalculate();

        result.Explainability.Should().NotBeEmpty();
        result.Audit.RuleSetVersions.Should().Contain(v => v.StartsWith("federal-2026"));
    }

    [Fact]
    public void Editing_an_earning_changes_gross_pay_after_recalc()
    {
        var state = NewState();
        var before = state.Recalculate().GrossPay;

        state.AddEarning(new EarningLineInput(EarningType.Bonus, Money.Usd(1000m), IsSupplemental: true));
        var after = state.Recalculate().GrossPay;

        (after - before).Amount.Should().Be(1000m);
    }

    [Fact]
    public void Reset_clears_earnings_and_deductions_and_invalidates_result()
    {
        var state = NewState();
        state.AddDeduction(new DeductionInput(DeductionType.HsaEmployee, DeductionAmountType.FixedPerPeriod, 50m));
        state.Recalculate();

        state.Reset();

        state.Deductions.Should().BeEmpty();
        state.Earnings.Should().HaveCount(1);
        state.LatestResult.Should().BeNull();
    }
}
