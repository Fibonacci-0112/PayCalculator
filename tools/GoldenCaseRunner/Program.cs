using System.Text.Json;
using PaycheckCalculator.Core.Calculations;
using PaycheckCalculator.Core.Earnings;
using PaycheckCalculator.Core.Household;
using PaycheckCalculator.Core.Jurisdictions;
using PaycheckCalculator.Core.ValueObjects;
using PaycheckCalculator.Core.W4;
using PaycheckCalculator.Core.Ytd;
using PaycheckCalculator.Payroll.Engine;
using PaycheckCalculator.TaxRules.Registry;

var casesDir = args.Length > 0 ? args[0] : Path.Combine(Directory.GetCurrentDirectory(), "tests/PaycheckCalculator.Tests.Golden/Cases");
if (!Directory.Exists(casesDir))
{
    Console.Error.WriteLine($"Cases directory not found: {casesDir}");
    return 1;
}

var pipeline = new PaycheckPipeline();
var registry = new InMemoryRulePackageRegistry();
var failures = 0;
var passes = 0;
foreach (var file in Directory.EnumerateFiles(casesDir, "*.json"))
{
    var json = File.ReadAllText(file);
    using var doc = JsonDocument.Parse(json);
    var root = doc.RootElement;
    var caseId = root.GetProperty("caseId").GetString()!;
    var taxYear = root.GetProperty("taxYear").GetInt32();
    var input = root.GetProperty("input");
    var expected = root.GetProperty("expected");
    var tolerance = root.GetProperty("tolerance").GetDecimal();

    var filing = Enum.Parse<FilingStatus>(input.GetProperty("filingStatus").GetString()!);
    var pf = Enum.Parse<PayFrequency>(input.GetProperty("payFrequency").GetString()!);
    var wages = input.GetProperty("regularWages").GetDecimal();

    var result = pipeline.Calculate(new PaycheckInput(
        Guid.NewGuid(), new TaxYear(taxYear), pf, WorkerType.SalariedW2,
        new[] { new EarningLineInput(EarningType.RegularSalary, Money.Usd(wages)) },
        Array.Empty<PaycheckCalculator.Core.Deductions.DeductionInput>(),
        W4Profile.Default(filing),
        HouseholdTaxProfile.Default(filing),
        JurisdictionContext.FederalOnly(),
        YtdSnapshot.Empty(new TaxYear(taxYear)),
        RoundingPolicy.CurrencyHalfAwayFromZeroToCent,
        new DateOnly(taxYear, 1, 15)),
        registry.GetBundle(new TaxYear(taxYear)));

    var ss = result.Taxes.Single(t => t.TaxType == "SocialSecurity").TaxAmount.Amount;
    var medicare = result.Taxes.Single(t => t.TaxType == "Medicare").TaxAmount.Amount;
    var expectedSs = expected.GetProperty("socialSecurityTax").GetDecimal();
    var expectedMed = expected.GetProperty("medicareTax").GetDecimal();
    if (Math.Abs(ss - expectedSs) <= tolerance && Math.Abs(medicare - expectedMed) <= tolerance)
    {
        Console.WriteLine($"PASS {caseId}");
        passes++;
    }
    else
    {
        Console.WriteLine($"FAIL {caseId} (ss={ss} expected={expectedSs}; medicare={medicare} expected={expectedMed})");
        failures++;
    }
}
Console.WriteLine($"\n{passes} passed, {failures} failed.");
return failures == 0 ? 0 : 2;
