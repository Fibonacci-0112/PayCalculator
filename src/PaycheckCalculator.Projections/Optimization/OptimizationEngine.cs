using PaycheckCalculator.Core.Calculations;
using PaycheckCalculator.Core.ValueObjects;

namespace PaycheckCalculator.Projections.Optimization;

public enum OptimizationGoal
{
    MaximizeTakeHomePay,
    BreakEvenAtTaxTime,
    TargetRefundAmount,
    AvoidOwingMoreThanThreshold,
    HitPriorYearSafeHarbor,
    HitCurrentYearSafeHarbor,
    IncreaseRetirementSavings,
    IncreaseHsaContribution,
    ReduceOverWithholding
}

public sealed record OptimizationRequest(
    OptimizationGoal Goal,
    Money TargetRefundOrDue,
    int RemainingPayPeriods,
    AnnualProjectionSnapshot CurrentProjection);

public sealed record OptimizationSuggestion(
    OptimizationGoal Goal,
    string RecommendationText,
    Money? ExtraFederalWithholdingPerPeriod,
    Money? ExtraStateWithholdingPerPeriod,
    Money? QuarterlyEstimatedPayment,
    decimal? NewRetirementContributionPercent,
    Money? NewHsaContributionPerPeriod,
    Money ProjectedRefundOrDueAfterChange,
    ConfidenceLevel Confidence,
    IReadOnlyList<string> Notes);

public sealed class OptimizationEngine
{
    public OptimizationSuggestion Suggest(OptimizationRequest request)
    {
        var snapshot = request.CurrentProjection;
        var projectedGap = snapshot.ProjectedFederalTax - snapshot.ProjectedFederalWithholding
                           - snapshot.ProjectedEstimatedPayments - request.TargetRefundOrDue;

        Money? extraWithholding = null;
        Money? quarterly = null;
        var notes = new List<string>();

        if (request.RemainingPayPeriods > 0)
        {
            var perPeriod = Math.Max(0m, projectedGap.Amount / request.RemainingPayPeriods);
            extraWithholding = Money.Usd(Math.Round(perPeriod, 2, MidpointRounding.AwayFromZero));
            notes.Add($"Distribute remaining projected gap across {request.RemainingPayPeriods} pay periods.");
        }
        else
        {
            quarterly = projectedGap.Amount > 0m ? projectedGap : Money.Zero;
            notes.Add("No remaining periods this year; quarterly estimated payment suggested.");
        }

        var projectedAfter = snapshot.ProjectedRefundOrAmountDue
            + (extraWithholding ?? Money.Zero) * request.RemainingPayPeriods
            + (quarterly ?? Money.Zero);

        return new OptimizationSuggestion(
            Goal: request.Goal,
            RecommendationText: BuildText(request.Goal, extraWithholding, quarterly),
            ExtraFederalWithholdingPerPeriod: extraWithholding,
            ExtraStateWithholdingPerPeriod: null,
            QuarterlyEstimatedPayment: quarterly,
            NewRetirementContributionPercent: null,
            NewHsaContributionPerPeriod: null,
            ProjectedRefundOrDueAfterChange: projectedAfter,
            Confidence: snapshot.Confidence,
            Notes: notes);
    }

    private static string BuildText(OptimizationGoal goal, Money? extra, Money? quarterly) => goal switch
    {
        OptimizationGoal.BreakEvenAtTaxTime when extra is not null =>
            $"Increase per-period federal withholding by {extra}.",
        OptimizationGoal.BreakEvenAtTaxTime when quarterly is not null =>
            $"Make a quarterly estimated payment of {quarterly}.",
        OptimizationGoal.TargetRefundAmount when extra is not null =>
            $"Add {extra} per period to reach the target refund.",
        _ => "Review projection and adjust W-4 or estimated payments to align with the chosen goal."
    };
}
