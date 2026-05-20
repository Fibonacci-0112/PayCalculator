using PaycheckCalculator.Core.Calculations;

namespace PaycheckCalculator.Reports;

public sealed record PaycheckReportModel(
    string ReportId,
    DateTimeOffset GeneratedAt,
    string AppVersion,
    string RuleSetVersion,
    PaycheckInput InputSummary,
    CalculationResult Result,
    IReadOnlyList<ExplainLine> Explainability,
    IReadOnlyList<DiagnosticWarning> Warnings,
    string DisclaimerText)
{
    public const string DefaultDisclaimer =
        "Estimates only. This report is not professional tax advice. Verify amounts with your employer or a tax professional before relying on them.";
}

public sealed record AnnualProjectionReportModel(
    string ReportId,
    DateTimeOffset GeneratedAt,
    AnnualProjectionSnapshot Snapshot,
    IReadOnlyList<DiagnosticWarning> Warnings,
    string DisclaimerText);

public sealed record AccountantPacketModel(
    string PacketId,
    DateTimeOffset GeneratedAt,
    int TaxYear,
    IReadOnlyList<string> RuleSetVersions,
    PaycheckReportModel LatestPaycheck,
    AnnualProjectionReportModel Projection,
    string DisclaimerText);
