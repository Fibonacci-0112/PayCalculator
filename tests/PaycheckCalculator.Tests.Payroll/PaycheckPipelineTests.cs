using FluentAssertions;
using PaycheckCalculator.Core.Calculations;
using PaycheckCalculator.Core.Deductions;
using PaycheckCalculator.Core.Earnings;
using PaycheckCalculator.Core.Household;
using PaycheckCalculator.Core.Jurisdictions;
using PaycheckCalculator.Core.ValueObjects;
using PaycheckCalculator.Core.W4;
using PaycheckCalculator.Core.Ytd;
using PaycheckCalculator.Payroll.Engine;
using PaycheckCalculator.TaxRules.Registry;
using Xunit;

namespace PaycheckCalculator.Tests.Payroll;

public class PaycheckPipelineTests
{
    private readonly PaycheckPipeline _pipeline = new();
    private readonly InMemoryRulePackageRegistry _registry = new();

    [Fact]
    public void Single_filer_biweekly_2500_per_period_computes_expected_fica()
    {
        var input = NewInput(FilingStatus.Single, PayFrequency.Biweekly, regular: 2500m);

        var result = _pipeline.Calculate(input, _registry.GetBundle(new TaxYear(2026)));

        result.GrossPay.Amount.Should().Be(2500m);
        result.SocialSecurityWages.Amount.Should().Be(2500m);
        result.MedicareWages.Amount.Should().Be(2500m);
        var ss = result.Taxes.Single(t => t.TaxType == "SocialSecurity");
        ss.TaxAmount.Amount.Should().Be(decimal.Round(2500m * 0.062m, 2, MidpointRounding.AwayFromZero));
        var medicare = result.Taxes.Single(t => t.TaxType == "Medicare");
        medicare.TaxAmount.Amount.Should().Be(decimal.Round(2500m * 0.0145m, 2, MidpointRounding.AwayFromZero));
    }

    [Fact]
    public void Pre_tax_401k_reduces_federal_taxable_wages_but_not_fica_wages()
    {
        var input = NewInput(FilingStatus.Single, PayFrequency.Biweekly, regular: 2500m,
            deductions: new[] { new DeductionInput(DeductionType.Traditional401k, DeductionAmountType.PercentOfGross, 10m) });

        var result = _pipeline.Calculate(input, _registry.GetBundle(new TaxYear(2026)));

        result.FederalTaxableWages.Amount.Should().Be(2250m);
        result.SocialSecurityWages.Amount.Should().Be(2500m);
        result.MedicareWages.Amount.Should().Be(2500m);
    }

    [Fact]
    public void Hsa_is_pre_tax_for_federal_state_ss_and_medicare()
    {
        var input = NewInput(FilingStatus.Single, PayFrequency.Biweekly, regular: 2500m,
            deductions: new[] { new DeductionInput(DeductionType.HsaEmployee, DeductionAmountType.FixedPerPeriod, 100m) });

        var result = _pipeline.Calculate(input, _registry.GetBundle(new TaxYear(2026)));

        result.FederalTaxableWages.Amount.Should().Be(2400m);
        result.SocialSecurityWages.Amount.Should().Be(2400m);
        result.MedicareWages.Amount.Should().Be(2400m);
    }

    [Fact]
    public void Bonus_paid_alone_uses_22_percent_supplemental_flat_rate()
    {
        var input = NewInput(FilingStatus.Single, PayFrequency.Biweekly, regular: 0m, bonus: 5000m);

        var result = _pipeline.Calculate(input, _registry.GetBundle(new TaxYear(2026)));

        var supplemental = result.Taxes.Single(t => t.TaxType == "FederalSupplemental");
        supplemental.TaxAmount.Amount.Should().Be(1100m); // 5000 * 0.22
        result.Warnings.Should().Contain(w => w.Category == WarningCategory.SupplementalWageMethodApplied);
    }

    [Fact]
    public void Social_security_wage_base_caps_ss_tax_when_ytd_exceeds_base()
    {
        var ytd = YtdSnapshot.Empty(new TaxYear(2026)) with
        {
            SocialSecurityWages = Money.Usd(184_000m)
        };
        var input = NewInput(FilingStatus.Single, PayFrequency.Biweekly, regular: 2500m) with { Ytd = ytd };

        var result = _pipeline.Calculate(input, _registry.GetBundle(new TaxYear(2026)));

        var ss = result.Taxes.Single(t => t.TaxType == "SocialSecurity");
        ss.TaxableWages.Amount.Should().Be(500m); // wage base remaining
        ss.TaxAmount.Amount.Should().Be(decimal.Round(500m * 0.062m, 2, MidpointRounding.AwayFromZero));
    }

    [Fact]
    public void Federal_withholding_for_zero_wages_is_zero()
    {
        var input = NewInput(FilingStatus.Single, PayFrequency.Biweekly, regular: 0m);

        var result = _pipeline.Calculate(input, _registry.GetBundle(new TaxYear(2026)));

        var fit = result.Taxes.Single(t => t.TaxType == "FederalIncomeTax");
        fit.TaxAmount.Amount.Should().Be(0m);
    }

    [Fact]
    public void Federal_withholding_uses_standard_deduction_so_low_income_owes_nothing()
    {
        // $400/week * 52 = $20,800 annualized. After $15,750 standard deduction, $5,050 taxable, all at 10% = $505.
        var input = NewInput(FilingStatus.Single, PayFrequency.Weekly, regular: 400m);

        var result = _pipeline.Calculate(input, _registry.GetBundle(new TaxYear(2026)));

        var fit = result.Taxes.Single(t => t.TaxType == "FederalIncomeTax");
        var expectedPerPeriod = decimal.Round(505m / 52m, 2, MidpointRounding.AwayFromZero);
        fit.TaxAmount.Amount.Should().Be(expectedPerPeriod);
    }

    [Fact]
    public void Married_filing_jointly_pays_less_federal_than_single_at_same_wages()
    {
        var mfj = NewInput(FilingStatus.MarriedFilingJointly, PayFrequency.Biweekly, regular: 3000m);
        var single = NewInput(FilingStatus.Single, PayFrequency.Biweekly, regular: 3000m);

        var mfjResult = _pipeline.Calculate(mfj, _registry.GetBundle(new TaxYear(2026)));
        var singleResult = _pipeline.Calculate(single, _registry.GetBundle(new TaxYear(2026)));

        var mfjFit = mfjResult.Taxes.Single(t => t.TaxType == "FederalIncomeTax").TaxAmount;
        var singleFit = singleResult.Taxes.Single(t => t.TaxType == "FederalIncomeTax").TaxAmount;
        mfjFit.Amount.Should().BeLessThan(singleFit.Amount);
    }

    [Fact]
    public void Step4c_extra_withholding_is_added_to_per_period_amount()
    {
        var baseInput = NewInput(FilingStatus.Single, PayFrequency.Biweekly, regular: 2500m);
        var withExtra = baseInput with { W4 = baseInput.W4! with { Step4cExtraWithholdingPerPeriod = Money.Usd(50m) } };

        var baseResult = _pipeline.Calculate(baseInput, _registry.GetBundle(new TaxYear(2026)));
        var extraResult = _pipeline.Calculate(withExtra, _registry.GetBundle(new TaxYear(2026)));

        var baseFit = baseResult.Taxes.Single(t => t.TaxType == "FederalIncomeTax").TaxAmount.Amount;
        var extraFit = extraResult.Taxes.Single(t => t.TaxType == "FederalIncomeTax").TaxAmount.Amount;
        (extraFit - baseFit).Should().Be(50m);
    }

    [Fact]
    public void Extended_pay_periods_27_biweekly_triggers_warning()
    {
        var input = NewInput(FilingStatus.Single, PayFrequency.Biweekly27, regular: 2500m);

        var result = _pipeline.Calculate(input, _registry.GetBundle(new TaxYear(2026)));

        result.Warnings.Should().Contain(w => w.Category == WarningCategory.AnnualPayPeriodOverrideApplied);
        result.Audit.AnnualPayPeriods.Should().Be(27);
    }

    [Fact]
    public void State_not_in_bundle_emits_unverified_jurisdiction_warning()
    {
        var input = NewInput(FilingStatus.Single, PayFrequency.Biweekly, regular: 2500m,
            jurisdiction: JurisdictionContext.FederalOnly() with { ResidentStateCode = "CA" });

        var result = _pipeline.Calculate(input, _registry.GetBundle(new TaxYear(2026)));

        result.Warnings.Should().Contain(w => w.Category == WarningCategory.JurisdictionUnverified);
    }

    [Fact]
    public void Illinois_resident_has_state_income_tax_line_and_no_unverified_warning()
    {
        var input = NewInput(FilingStatus.Single, PayFrequency.Biweekly, regular: 2500m,
            jurisdiction: JurisdictionContext.FederalOnly() with { ResidentStateCode = "IL" });

        var result = _pipeline.Calculate(input, _registry.GetBundle(new TaxYear(2026)));

        var state = result.Taxes.Single(t => t.TaxType == "StateIncomeTax");
        // annual 65,000 - 2,925 IL exemption = 62,075 * 4.95% = 3,072.7125, / 26 = 118.18
        var expected = decimal.Round((65_000m - 2_925m) * 0.0495m / 26m, 2, MidpointRounding.AwayFromZero);
        state.TaxAmount.Amount.Should().Be(expected);
        state.Authority.AuthorityType.Should().Be(TaxAuthorityType.StateIncomeTax);
        state.SupportLevel.Should().Be(TaxRuleSupportLevel.Estimated);
        state.Explanation.Should().NotBeEmpty();
        result.Warnings.Should().NotContain(w => w.Category == WarningCategory.JurisdictionUnverified);
    }

    [Fact]
    public void Pennsylvania_taxes_compensation_flat_with_no_standard_deduction()
    {
        var input = NewInput(FilingStatus.Single, PayFrequency.Biweekly, regular: 2500m,
            jurisdiction: JurisdictionContext.FederalOnly() with { ResidentStateCode = "PA" });

        var result = _pipeline.Calculate(input, _registry.GetBundle(new TaxYear(2026)));

        var state = result.Taxes.Single(t => t.TaxType == "StateIncomeTax");
        state.TaxAmount.Amount.Should().Be(decimal.Round(2500m * 0.0307m, 2, MidpointRounding.AwayFromZero));
    }

    [Fact]
    public void State_income_tax_flows_into_total_state_and_local_and_ytd()
    {
        var input = NewInput(FilingStatus.Single, PayFrequency.Biweekly, regular: 2500m,
            jurisdiction: JurisdictionContext.FederalOnly() with { ResidentStateCode = "PA" });

        var result = _pipeline.Calculate(input, _registry.GetBundle(new TaxYear(2026)));

        var stateTax = result.Taxes.Single(t => t.TaxType == "StateIncomeTax").TaxAmount.Amount;
        stateTax.Should().BeGreaterThan(0m);
        result.TotalStateAndLocal.Amount.Should().Be(stateTax);
        result.YtdDelta.StateWithholding.Amount.Should().Be(stateTax);
        result.UpdatedYtd.StateWithholding.Amount.Should().Be(stateTax);
    }

    [Fact]
    public void Illinois_conforms_to_federal_so_pre_tax_401k_reduces_state_taxable_wages()
    {
        var input = NewInput(FilingStatus.Single, PayFrequency.Biweekly, regular: 2500m,
            deductions: new[] { new DeductionInput(DeductionType.Traditional401k, DeductionAmountType.PercentOfGross, 10m) },
            jurisdiction: JurisdictionContext.FederalOnly() with { ResidentStateCode = "IL" });

        var result = _pipeline.Calculate(input, _registry.GetBundle(new TaxYear(2026)));

        result.StateTaxableWages.Amount.Should().Be(2250m);
        var state = result.Taxes.Single(t => t.TaxType == "StateIncomeTax");
        state.TaxableWages.Amount.Should().Be(2250m);
        // annual 58,500 - 2,925 exemption = 55,575 * 4.95% / 26
        var expected = decimal.Round((2250m * 26 - 2_925m) * 0.0495m / 26m, 2, MidpointRounding.AwayFromZero);
        state.TaxAmount.Amount.Should().Be(expected);
    }

    [Fact]
    public void Pennsylvania_taxes_401k_deferrals_so_they_do_not_reduce_state_wages()
    {
        var input = NewInput(FilingStatus.Single, PayFrequency.Biweekly, regular: 2500m,
            deductions: new[] { new DeductionInput(DeductionType.Traditional401k, DeductionAmountType.PercentOfGross, 10m) },
            jurisdiction: JurisdictionContext.FederalOnly() with { ResidentStateCode = "PA" });

        var result = _pipeline.Calculate(input, _registry.GetBundle(new TaxYear(2026)));

        // PA taxes the 401(k) deferral, so the state base is the full gross, not the reduced bucket.
        var state = result.Taxes.Single(t => t.TaxType == "StateIncomeTax");
        state.TaxableWages.Amount.Should().Be(2500m);
        state.TaxAmount.Amount.Should().Be(decimal.Round(2500m * 0.0307m, 2, MidpointRounding.AwayFromZero));
        result.StateTaxableWages.Amount.Should().Be(2500m);
    }

    [Fact]
    public void Result_contains_explainability_for_every_tax_line()
    {
        var input = NewInput(FilingStatus.Single, PayFrequency.Biweekly, regular: 2500m);

        var result = _pipeline.Calculate(input, _registry.GetBundle(new TaxYear(2026)));

        foreach (var tax in result.Taxes)
        {
            tax.Explanation.Should().NotBeEmpty();
            tax.Explanation.Should().AllSatisfy(e =>
            {
                e.RuleSetVersion.Should().NotBeNullOrWhiteSpace();
                e.SourceForms.Should().NotBeEmpty();
                e.Inputs.Should().NotBeEmpty();
            });
        }
    }

    [Fact]
    public void Audit_captures_engine_version_rule_set_versions_and_rounding()
    {
        var input = NewInput(FilingStatus.Single, PayFrequency.Biweekly, regular: 2500m);

        var result = _pipeline.Calculate(input, _registry.GetBundle(new TaxYear(2026)));

        result.Audit.EngineVersion.Should().Be(PaycheckPipeline.EngineVersion);
        result.Audit.RuleSetVersions.Should().Contain(v => v.StartsWith("federal-2026"));
        result.Audit.RoundingPolicy.Name.Should().Be("CurrencyRoundHalfAwayFromZeroToCent");
    }

    private static PaycheckInput NewInput(
        FilingStatus filingStatus,
        PayFrequency frequency,
        decimal regular,
        decimal bonus = 0m,
        IReadOnlyList<DeductionInput>? deductions = null,
        JurisdictionContext? jurisdiction = null)
    {
        var earnings = new List<EarningLineInput>();
        if (regular > 0m) earnings.Add(new EarningLineInput(EarningType.RegularSalary, Money.Usd(regular)));
        if (bonus > 0m) earnings.Add(new EarningLineInput(EarningType.Bonus, Money.Usd(bonus), IsSupplemental: true));

        return new PaycheckInput(
            ScenarioId: Guid.NewGuid(),
            TaxYear: new TaxYear(2026),
            PayFrequency: frequency,
            WorkerType: WorkerType.SalariedW2,
            Earnings: earnings,
            Deductions: deductions ?? Array.Empty<DeductionInput>(),
            W4: W4Profile.Default(filingStatus),
            Household: HouseholdTaxProfile.Default(filingStatus),
            Jurisdictions: jurisdiction ?? JurisdictionContext.FederalOnly(),
            Ytd: YtdSnapshot.Empty(new TaxYear(2026)),
            RoundingPolicy: RoundingPolicy.CurrencyHalfAwayFromZeroToCent,
            PayDate: new DateOnly(2026, 1, 15));
    }
}
