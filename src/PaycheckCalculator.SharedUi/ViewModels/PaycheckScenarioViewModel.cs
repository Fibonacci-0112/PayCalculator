using PaycheckCalculator.Core.Deductions;
using PaycheckCalculator.Core.Earnings;
using PaycheckCalculator.Core.Household;
using PaycheckCalculator.Core.Jurisdictions;
using PaycheckCalculator.Core.ValueObjects;
using PaycheckCalculator.Core.W4;
using PaycheckCalculator.Core.Ytd;
using PaycheckCalculator.Core.Calculations;

namespace PaycheckCalculator.SharedUi.ViewModels;

public sealed class PaycheckScenarioViewModel
{
    public Guid ScenarioId { get; set; } = Guid.NewGuid();
    public int TaxYear { get; set; } = 2026;
    public PayFrequency PayFrequency { get; set; } = PayFrequency.Biweekly;
    public WorkerType WorkerType { get; set; } = WorkerType.SalariedW2;
    public FilingStatus FilingStatus { get; set; } = FilingStatus.Single;
    public bool MultipleJobs { get; set; }
    public decimal DependentsCredit { get; set; }
    public decimal Step4aOther { get; set; }
    public decimal Step4bDeductions { get; set; }
    public decimal Step4cExtraPerPeriod { get; set; }

    public decimal RegularWages { get; set; } = 2500m;
    public decimal OvertimeWages { get; set; }
    public decimal BonusWages { get; set; }

    public decimal Traditional401kPercent { get; set; }
    public decimal HsaPerPeriod { get; set; }
    public decimal HealthInsurancePerPeriod { get; set; }

    public string? HomeStateCode { get; set; }

    public decimal YtdGross { get; set; }
    public decimal YtdFederalWithholding { get; set; }
    public decimal YtdSocialSecurityWages { get; set; }
    public decimal YtdMedicareWages { get; set; }
    public int CompletedPayPeriods { get; set; }

    public DateOnly PayDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    public PaycheckInput ToInput()
    {
        var earnings = new List<EarningLineInput>();
        if (RegularWages > 0m)
            earnings.Add(new EarningLineInput(EarningType.RegularSalary, Money.Usd(RegularWages)));
        if (OvertimeWages > 0m)
            earnings.Add(new EarningLineInput(EarningType.Overtime, Money.Usd(OvertimeWages)));
        if (BonusWages > 0m)
            earnings.Add(new EarningLineInput(EarningType.Bonus, Money.Usd(BonusWages), IsSupplemental: true));

        var deductions = new List<DeductionInput>();
        if (Traditional401kPercent > 0m)
            deductions.Add(new DeductionInput(DeductionType.Traditional401k, DeductionAmountType.PercentOfGross, Traditional401kPercent));
        if (HsaPerPeriod > 0m)
            deductions.Add(new DeductionInput(DeductionType.HsaEmployee, DeductionAmountType.FixedPerPeriod, HsaPerPeriod));
        if (HealthInsurancePerPeriod > 0m)
            deductions.Add(new DeductionInput(DeductionType.HealthInsuranceCafeteria, DeductionAmountType.FixedPerPeriod, HealthInsurancePerPeriod));

        var w4 = new W4Profile(
            FilingStatus,
            MultipleJobs,
            Money.Usd(DependentsCredit),
            Money.Usd(Step4aOther),
            Money.Usd(Step4bDeductions),
            Money.Usd(Step4cExtraPerPeriod));

        var household = HouseholdTaxProfile.Default(FilingStatus);

        var jurisdictions = string.IsNullOrWhiteSpace(HomeStateCode)
            ? JurisdictionContext.FederalOnly()
            : JurisdictionContext.FederalOnly() with { ResidentStateCode = HomeStateCode!.ToUpperInvariant() };

        var ytd = YtdSnapshot.Empty(new TaxYear(TaxYear)) with
        {
            GrossWages = Money.Usd(YtdGross),
            FederalWithholding = Money.Usd(YtdFederalWithholding),
            SocialSecurityWages = Money.Usd(YtdSocialSecurityWages),
            MedicareWages = Money.Usd(YtdMedicareWages),
            CompletedPayPeriods = CompletedPayPeriods
        };

        return new PaycheckInput(
            ScenarioId,
            new TaxYear(TaxYear),
            PayFrequency,
            WorkerType,
            earnings,
            deductions,
            w4,
            household,
            jurisdictions,
            ytd,
            RoundingPolicy.CurrencyHalfAwayFromZeroToCent,
            PayDate);
    }
}
