using PaycheckCalculator.Core.Calculations;
using PaycheckCalculator.Core.Deductions;
using PaycheckCalculator.Core.Earnings;
using PaycheckCalculator.Core.ValueObjects;
using PaycheckCalculator.Core.W4;
using PaycheckCalculator.Core.Ytd;
using PaycheckCalculator.Payroll.Deductions;
using PaycheckCalculator.Payroll.Earnings;
using PaycheckCalculator.Payroll.Federal;
using PaycheckCalculator.TaxRules.Model;

namespace PaycheckCalculator.Payroll.Engine;

public sealed class PaycheckPipeline : IPaycheckCalculator
{
    public const string EngineVersion = "1.0.0";

    private readonly EarningsNormalizer _normalizer = new();
    private readonly FederalWithholdingCalculator _federal = new();
    private readonly FicaCalculator _fica = new();
    private readonly SupplementalWageCalculator _supplemental = new();

    public CalculationResult Calculate(PaycheckInput input, TaxRuleSetBundle rules)
    {
        var explain = new ExplainabilityWriter();
        var warnings = new List<DiagnosticWarning>();

        // 1. Normalize earnings.
        var earnings = _normalizer.Normalize(input.Earnings);
        var grossPay = earnings.Aggregate(Money.Zero, (acc, e) => acc + e.Amount);
        var supplementalAmount = earnings
            .Where(e => e.IsSupplemental)
            .Aggregate(Money.Zero, (acc, e) => acc + e.Amount);
        var regularAmount = grossPay - supplementalAmount;

        // 2/3. Classify deductions and apply pre-tax reductions.
        var perPeriodDeductions = ResolveDeductionAmounts(input.Deductions, grossPay, input.RoundingPolicy);
        var deductionResults = new List<DeductionResult>();
        var preTaxFederal = Money.Zero;
        var preTaxSocialSecurity = Money.Zero;
        var preTaxMedicare = Money.Zero;
        var preTaxState = Money.Zero;
        var preTaxLocal = Money.Zero;
        foreach (var (deduction, amount) in perPeriodDeductions)
        {
            var treatment = deduction.OverrideTreatment ?? DeductionClassifier.Classify(deduction.Type);
            if (treatment.FederalIncomeTax == TaxTreatment.Reduces) preTaxFederal += amount;
            if (treatment.SocialSecurity == TaxTreatment.Reduces) preTaxSocialSecurity += amount;
            if (treatment.Medicare == TaxTreatment.Reduces) preTaxMedicare += amount;
            if (treatment.State == TaxTreatment.Reduces) preTaxState += amount;
            if (treatment.Local == TaxTreatment.Reduces) preTaxLocal += amount;
            deductionResults.Add(new DeductionResult(
                deduction.Type,
                deduction.Label ?? deduction.Type.ToString(),
                amount,
                treatment,
                IsPreTaxForFederal: treatment.FederalIncomeTax == TaxTreatment.Reduces,
                Warnings: Array.Empty<DiagnosticWarning>()));
        }

        // 4. Build taxable wage buckets.
        var federalTaxable = Money.Max(Money.Zero, grossPay - preTaxFederal);
        var socialSecurityWages = Money.Max(Money.Zero, grossPay - preTaxSocialSecurity);
        var medicareWages = Money.Max(Money.Zero, grossPay - preTaxMedicare);
        var stateTaxable = Money.Max(Money.Zero, grossPay - preTaxState);
        var localTaxable = Money.Max(Money.Zero, grossPay - preTaxLocal);

        // 5. Federal withholding on regular wages.
        var w4 = input.W4 ?? W4Profile.Default(input.Household.FilingStatus);
        var annualPayPeriods = input.PayFrequency.AnnualPeriods();
        if (input.PayFrequency.IsExtendedPeriod())
        {
            warnings.Add(new DiagnosticWarning(
                WarningCategory.AnnualPayPeriodOverrideApplied, WarningSeverity.Info,
                $"Annualization is using {annualPayPeriods} pay periods for an extended-year frequency."));
        }
        var regularFederalTaxable = Money.Max(Money.Zero, regularAmount - preTaxFederal);
        var federalRegular = _federal.Calculate(new FederalWithholdingContext(
            regularFederalTaxable, annualPayPeriods, w4, input.TaxYear, input.RoundingPolicy, rules.Federal), explain);
        var federalSupplemental = _supplemental.CalculateFlatSupplemental(new SupplementalWageContext(
            supplementalAmount, Money.Zero, input.TaxYear, input.RoundingPolicy, rules.Federal.RuleSetVersion), explain, warnings);

        // 6. FICA + Additional Medicare.
        var ficaResults = _fica.Calculate(new FicaContext(
            socialSecurityWages, medicareWages, input.Ytd, w4,
            input.TaxYear, input.RoundingPolicy, rules.Federal.RuleSetVersion), explain, warnings);

        // 7. State + local: rule packages are wired in by the StateLocalTaxEngine in
        // PaycheckCalculator.Jurisdictions when present. For MVP we contribute zero state/local tax
        // and surface a warning so the UI can flag it as Manual/Unverified.
        var stateLocalResults = new List<TaxLineResult>();
        if (!string.IsNullOrEmpty(input.Jurisdictions.ResidentStateCode) &&
            input.Jurisdictions.ResidentStateCode != "US" &&
            !rules.States.ContainsKey(input.Jurisdictions.ResidentStateCode))
        {
            warnings.Add(new DiagnosticWarning(
                WarningCategory.JurisdictionUnverified, WarningSeverity.Warning,
                $"State withholding for {input.Jurisdictions.ResidentStateCode} is not in the installed rule bundle. " +
                "Enter a manual rate to include state tax.",
                new Dictionary<string, string> { ["state"] = input.Jurisdictions.ResidentStateCode }));
        }

        // Assemble tax lines.
        var allTaxes = new List<TaxLineResult> { federalRegular };
        if (federalSupplemental is not null) allTaxes.Add(federalSupplemental);
        allTaxes.AddRange(ficaResults);
        allTaxes.AddRange(stateLocalResults);

        var totalTax = allTaxes.Aggregate(Money.Zero, (acc, t) => acc + t.TaxAmount);
        var totalDeductions = perPeriodDeductions.Aggregate(Money.Zero, (acc, d) => acc + d.amount);
        var netPay = grossPay - totalTax - totalDeductions;

        var federalWithholdingTotal = federalRegular.TaxAmount + (federalSupplemental?.TaxAmount ?? Money.Zero);
        var ssTax = ficaResults.FirstOrDefault(r => r.TaxType == "SocialSecurity")?.TaxAmount ?? Money.Zero;
        var medicareTax = ficaResults.FirstOrDefault(r => r.TaxType == "Medicare")?.TaxAmount ?? Money.Zero;
        var addlMedicareTax = ficaResults.FirstOrDefault(r => r.TaxType == "AdditionalMedicare")?.TaxAmount ?? Money.Zero;

        var ytdDelta = new YtdDelta(
            PayDate: input.PayDate,
            GrossWages: grossPay,
            FederalTaxableWages: federalTaxable,
            FederalWithholding: federalWithholdingTotal,
            SocialSecurityWages: socialSecurityWages,
            SocialSecurityTax: ssTax,
            MedicareWages: medicareWages,
            MedicareTax: medicareTax,
            AdditionalMedicareTax: addlMedicareTax,
            StateWages: stateTaxable,
            StateWithholding: Money.Zero,
            LocalWages: localTaxable,
            LocalWithholding: Money.Zero,
            PreTaxDeductions: preTaxFederal,
            PostTaxDeductions: totalDeductions - preTaxFederal);
        var updatedYtd = input.Ytd.Apply(ytdDelta);

        var projection = BuildProjection(input, updatedYtd, federalWithholdingTotal, annualPayPeriods);

        var audit = new CalculationAudit(
            GeneratedAt: DateTimeOffset.UtcNow,
            EngineVersion: EngineVersion,
            RuleSetVersions: rules.AllRuleSetVersions().ToArray(),
            RoundingPolicy: input.RoundingPolicy,
            TaxYear: input.TaxYear,
            PayFrequency: input.PayFrequency.ToString(),
            AnnualPayPeriods: annualPayPeriods);

        return new CalculationResult(
            ScenarioId: input.ScenarioId,
            PayDate: input.PayDate,
            GrossPay: grossPay,
            FederalTaxableWages: federalTaxable,
            SocialSecurityWages: socialSecurityWages,
            MedicareWages: medicareWages,
            StateTaxableWages: stateTaxable,
            LocalTaxableWages: localTaxable,
            Taxes: allTaxes,
            Deductions: deductionResults,
            NetPay: netPay,
            Projection: projection,
            YtdDelta: ytdDelta,
            UpdatedYtd: updatedYtd,
            Explainability: explain.Lines,
            Warnings: warnings,
            Audit: audit);
    }

    private static List<(DeductionInput deduction, Money amount)> ResolveDeductionAmounts(
        IReadOnlyList<DeductionInput> deductions, Money grossPay, RoundingPolicy rounding)
    {
        var list = new List<(DeductionInput, Money)>();
        foreach (var d in deductions)
        {
            var amount = d.AmountType switch
            {
                DeductionAmountType.FixedPerPeriod => Money.Usd(d.Amount),
                DeductionAmountType.PercentOfGross => Money.Usd(rounding.Round(grossPay.Amount * d.Amount / 100m)),
                DeductionAmountType.PercentOfNet => Money.Usd(rounding.Round(grossPay.Amount * d.Amount / 100m)),
                _ => Money.Zero
            };
            list.Add((d, amount));
        }
        return list;
    }

    private static AnnualProjectionSnapshot BuildProjection(
        PaycheckInput input, YtdSnapshot updatedYtd, Money federalWithholdingThisPeriod, int annualPayPeriods)
    {
        var completed = Math.Max(updatedYtd.CompletedPayPeriods, 1);
        var remaining = Math.Max(0, annualPayPeriods - completed);
        var projectedYearGross = updatedYtd.GrossWages + (updatedYtd.GrossWages / completed) * remaining;
        var projectedYearWithholding = updatedYtd.FederalWithholding + (updatedYtd.FederalWithholding / completed) * remaining;
        var projectedYearTaxable = updatedYtd.FederalTaxableWages + (updatedYtd.FederalTaxableWages / completed) * remaining;

        var w4 = input.W4 ?? W4Profile.Default(input.Household.FilingStatus);
        var standardDeduction = Money.Usd(TaxRules.Federal2026.FederalRule2026.StandardDeductionForWithholding[w4.FilingStatus]);
        var agi = projectedYearTaxable
                  + input.Household.OtherHouseholdWagesAnnual
                  + input.Household.OtherTaxableInterestAnnual
                  + input.Household.OtherDividendsAnnual
                  + input.Household.OtherCapitalGainsAnnual
                  + input.Household.RetirementIncomeAnnual
                  + input.Household.UnemploymentIncomeAnnual;
        var deductible = input.Household.EstimatedItemizedDeductionsAnnual >= standardDeduction
            ? input.Household.EstimatedItemizedDeductionsAnnual
            : standardDeduction;
        var taxableIncome = Money.Max(Money.Zero, agi - deductible);

        var brackets = TaxRules.Federal2026.FederalRule2026.StandardBracketsFor(w4.FilingStatus);
        var federalTaxAmount = ApplyBracketsToProjection(taxableIncome.Amount, brackets);
        var federalCredits = w4.Step3DependentsCredit;
        var federalTax = Money.Max(Money.Zero, Money.Usd(federalTaxAmount) - federalCredits);

        var refundOrDue = projectedYearWithholding + input.Household.EstimatedPaymentsMadeYtd - federalTax;
        var recommendedExtraPerPeriod = remaining > 0 && refundOrDue.Amount < 0
            ? Money.Usd(input.RoundingPolicy.Round(Math.Abs(refundOrDue.Amount) / remaining))
            : Money.Zero;

        var confidence = input.WorkerType switch
        {
            WorkerType.SalariedW2 => ConfidenceLevel.High,
            WorkerType.HourlyW2 => ConfidenceLevel.Medium,
            WorkerType.SelfEmployed => ConfidenceLevel.Low,
            WorkerType.Mixed => ConfidenceLevel.Low,
            _ => ConfidenceLevel.Medium
        };

        return new AnnualProjectionSnapshot(
            TaxYear: input.TaxYear,
            ProjectedGrossWages: projectedYearGross,
            ProjectedAdjustedGrossIncome: agi,
            ProjectedTaxableIncome: taxableIncome,
            ProjectedFederalTax: federalTax,
            ProjectedStateTax: Money.Zero,
            ProjectedFederalCredits: federalCredits,
            ProjectedFederalWithholding: projectedYearWithholding,
            ProjectedEstimatedPayments: input.Household.EstimatedPaymentsMadeYtd,
            ProjectedRefundOrAmountDue: refundOrDue,
            RecommendedExtraFederalWithholdingPerPeriod: recommendedExtraPerPeriod,
            RecommendedNextQuarterlyEstimatedPayment: Money.Zero,
            Confidence: confidence,
            AssumptionNotes: new[]
            {
                "Projection extrapolates current YTD across remaining pay periods.",
                $"Withholding this period: {federalWithholdingThisPeriod}.",
                "State and local taxes are not included in the federal-only projection."
            });
    }

    private static decimal ApplyBracketsToProjection(decimal taxableAmount, IReadOnlyList<TaxBracket> brackets)
    {
        foreach (var bracket in brackets)
        {
            if (!bracket.Ceiling.HasValue || taxableAmount <= bracket.Ceiling.Value)
                return bracket.BaseTax + (taxableAmount - bracket.Floor) * bracket.MarginalRate;
        }
        var last = brackets[^1];
        return last.BaseTax + (taxableAmount - last.Floor) * last.MarginalRate;
    }
}
