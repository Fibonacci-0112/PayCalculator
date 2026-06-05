using System.Text.Json;
using FluentAssertions;
using PaycheckCalculator.Core.Calculations;
using PaycheckCalculator.Core.Earnings;
using PaycheckCalculator.Core.Household;
using PaycheckCalculator.Core.Jurisdictions;
using PaycheckCalculator.Core.ValueObjects;
using PaycheckCalculator.Core.W4;
using PaycheckCalculator.Core.Ytd;
using PaycheckCalculator.Payroll.Engine;
using PaycheckCalculator.TaxRules.Registry;
using Xunit;

namespace PaycheckCalculator.Tests.Golden;

public sealed record GoldenCase(string CaseId, int TaxYear, GoldenInput Input, GoldenExpected Expected, decimal Tolerance, string? Jurisdiction = null);
public sealed record GoldenInput(string FilingStatus, string PayFrequency, decimal RegularWages);
public sealed record GoldenExpected(decimal SocialSecurityTax, decimal MedicareTax, decimal? StateIncomeTax = null);

public class GoldenCaseRunnerTests
{
    public static IEnumerable<object[]> Cases()
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "Cases");
        if (!Directory.Exists(dir)) yield break;
        foreach (var file in Directory.EnumerateFiles(dir, "*.json"))
        {
            yield return new object[] { file };
        }
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void Golden_case_matches_calculator_output(string casePath)
    {
        var json = File.ReadAllText(casePath);
        var golden = JsonSerializer.Deserialize<GoldenCase>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        })!;

        var jurisdiction = string.IsNullOrWhiteSpace(golden.Jurisdiction) || golden.Jurisdiction == "US"
            ? JurisdictionContext.FederalOnly()
            : JurisdictionContext.FederalOnly() with { ResidentStateCode = golden.Jurisdiction };

        var input = new PaycheckInput(
            ScenarioId: Guid.Parse("00000000-0000-0000-0000-000000000001"),
            TaxYear: new TaxYear(golden.TaxYear),
            PayFrequency: Enum.Parse<PayFrequency>(golden.Input.PayFrequency),
            WorkerType: WorkerType.SalariedW2,
            Earnings: new[] { new EarningLineInput(EarningType.RegularSalary, Money.Usd(golden.Input.RegularWages)) },
            Deductions: Array.Empty<PaycheckCalculator.Core.Deductions.DeductionInput>(),
            W4: W4Profile.Default(Enum.Parse<FilingStatus>(golden.Input.FilingStatus)),
            Household: HouseholdTaxProfile.Default(Enum.Parse<FilingStatus>(golden.Input.FilingStatus)),
            Jurisdictions: jurisdiction,
            Ytd: YtdSnapshot.Empty(new TaxYear(golden.TaxYear)),
            RoundingPolicy: RoundingPolicy.CurrencyHalfAwayFromZeroToCent,
            PayDate: new DateOnly(golden.TaxYear, 1, 15));

        var pipeline = new PaycheckPipeline();
        var registry = new InMemoryRulePackageRegistry();
        var result = pipeline.Calculate(input, registry.GetBundle(new TaxYear(golden.TaxYear)));

        var ss = result.Taxes.Single(t => t.TaxType == "SocialSecurity").TaxAmount.Amount;
        var medicare = result.Taxes.Single(t => t.TaxType == "Medicare").TaxAmount.Amount;
        ss.Should().BeApproximately(golden.Expected.SocialSecurityTax, golden.Tolerance);
        medicare.Should().BeApproximately(golden.Expected.MedicareTax, golden.Tolerance);

        if (golden.Expected.StateIncomeTax is { } expectedStateTax)
        {
            var stateTax = result.Taxes.Single(t => t.TaxType == "StateIncomeTax").TaxAmount.Amount;
            stateTax.Should().BeApproximately(expectedStateTax, golden.Tolerance);
        }
    }
}
