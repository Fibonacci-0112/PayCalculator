using FluentAssertions;
using PaycheckCalculator.Core.Calculations;
using PaycheckCalculator.Core.ValueObjects;
using PaycheckCalculator.Projections.Optimization;
using Xunit;

namespace PaycheckCalculator.Tests.Projections;

public class OptimizationEngineTests
{
    [Fact]
    public void Break_even_goal_with_remaining_periods_recommends_per_period_extra_withholding()
    {
        var snapshot = new AnnualProjectionSnapshot(
            new TaxYear(2026),
            ProjectedGrossWages: Money.Usd(80_000m),
            ProjectedAdjustedGrossIncome: Money.Usd(80_000m),
            ProjectedTaxableIncome: Money.Usd(64_250m),
            ProjectedFederalTax: Money.Usd(10_000m),
            ProjectedStateTax: Money.Zero,
            ProjectedFederalCredits: Money.Zero,
            ProjectedFederalWithholding: Money.Usd(8_000m),
            ProjectedEstimatedPayments: Money.Zero,
            ProjectedRefundOrAmountDue: Money.Usd(-2_000m),
            RecommendedExtraFederalWithholdingPerPeriod: Money.Zero,
            RecommendedNextQuarterlyEstimatedPayment: Money.Zero,
            Confidence: ConfidenceLevel.High,
            AssumptionNotes: Array.Empty<string>());

        var engine = new OptimizationEngine();
        var suggestion = engine.Suggest(new OptimizationRequest(
            OptimizationGoal.BreakEvenAtTaxTime, Money.Zero, RemainingPayPeriods: 10, snapshot));

        suggestion.ExtraFederalWithholdingPerPeriod.Should().NotBeNull();
        suggestion.ExtraFederalWithholdingPerPeriod!.Value.Amount.Should().Be(200m);
        suggestion.QuarterlyEstimatedPayment.Should().BeNull();
    }

    [Fact]
    public void No_remaining_periods_recommends_quarterly_estimated_payment()
    {
        var snapshot = new AnnualProjectionSnapshot(
            new TaxYear(2026),
            ProjectedGrossWages: Money.Usd(80_000m),
            ProjectedAdjustedGrossIncome: Money.Usd(80_000m),
            ProjectedTaxableIncome: Money.Usd(64_250m),
            ProjectedFederalTax: Money.Usd(10_000m),
            ProjectedStateTax: Money.Zero,
            ProjectedFederalCredits: Money.Zero,
            ProjectedFederalWithholding: Money.Usd(8_000m),
            ProjectedEstimatedPayments: Money.Zero,
            ProjectedRefundOrAmountDue: Money.Usd(-2_000m),
            RecommendedExtraFederalWithholdingPerPeriod: Money.Zero,
            RecommendedNextQuarterlyEstimatedPayment: Money.Zero,
            Confidence: ConfidenceLevel.High,
            AssumptionNotes: Array.Empty<string>());

        var engine = new OptimizationEngine();
        var suggestion = engine.Suggest(new OptimizationRequest(
            OptimizationGoal.BreakEvenAtTaxTime, Money.Zero, RemainingPayPeriods: 0, snapshot));

        suggestion.QuarterlyEstimatedPayment.Should().NotBeNull();
        suggestion.QuarterlyEstimatedPayment!.Value.Amount.Should().Be(2_000m);
    }
}
