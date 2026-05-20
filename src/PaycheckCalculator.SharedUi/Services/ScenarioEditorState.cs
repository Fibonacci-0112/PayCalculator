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

namespace PaycheckCalculator.SharedUi.Services;

/// <summary>
/// Holds the user's in-progress paycheck scenario across Expert Mode screens. Components subscribe to
/// <see cref="Changed"/> to re-render when other screens edit shared state. Calculation results are
/// computed lazily from <see cref="LatestResult"/>.
/// </summary>
public sealed class ScenarioEditorState
{
    private readonly IPaycheckCalculator _calculator;
    private readonly IRulePackageRegistry _rules;

    public ScenarioEditorState(IPaycheckCalculator calculator, IRulePackageRegistry rules)
    {
        _calculator = calculator;
        _rules = rules;
    }

    public event Action? Changed;

    public Guid ScenarioId { get; private set; } = Guid.NewGuid();
    public TaxYear TaxYear { get; set; } = new(2026);
    public PayFrequency PayFrequency { get; set; } = PayFrequency.Biweekly;
    public WorkerType WorkerType { get; set; } = WorkerType.SalariedW2;
    public DateOnly PayDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public string? HomeStateCode { get; set; }
    public RoundingPolicy RoundingPolicy { get; set; } = RoundingPolicy.CurrencyHalfAwayFromZeroToCent;

    public W4Profile W4 { get; private set; } = W4Profile.Default(FilingStatus.Single);
    public HouseholdTaxProfile Household { get; private set; } = HouseholdTaxProfile.Default(FilingStatus.Single);
    public YtdSnapshot Ytd { get; private set; } = YtdSnapshot.Empty(new TaxYear(2026));

    public List<EarningLineInput> Earnings { get; } = new()
    {
        new EarningLineInput(EarningType.RegularSalary, Money.Usd(2500m))
    };

    public List<DeductionInput> Deductions { get; } = new();

    public CalculationResult? LatestResult { get; private set; }

    public void UpdateW4(W4Profile w4)
    {
        W4 = w4;
        Household = Household with { FilingStatus = w4.FilingStatus };
        NotifyChanged();
    }

    public void UpdateHousehold(HouseholdTaxProfile household)
    {
        Household = household;
        W4 = W4 with { FilingStatus = household.FilingStatus };
        NotifyChanged();
    }

    public void UpdateYtd(YtdSnapshot ytd)
    {
        Ytd = ytd;
        NotifyChanged();
    }

    public void AddEarning(EarningLineInput earning)
    {
        Earnings.Add(earning);
        NotifyChanged();
    }

    public void RemoveEarning(int index)
    {
        if (index >= 0 && index < Earnings.Count)
        {
            Earnings.RemoveAt(index);
            NotifyChanged();
        }
    }

    public void ReplaceEarning(int index, EarningLineInput earning)
    {
        if (index >= 0 && index < Earnings.Count)
        {
            Earnings[index] = earning;
            NotifyChanged();
        }
    }

    public void AddDeduction(DeductionInput deduction)
    {
        Deductions.Add(deduction);
        NotifyChanged();
    }

    public void RemoveDeduction(int index)
    {
        if (index >= 0 && index < Deductions.Count)
        {
            Deductions.RemoveAt(index);
            NotifyChanged();
        }
    }

    public void ReplaceDeduction(int index, DeductionInput deduction)
    {
        if (index >= 0 && index < Deductions.Count)
        {
            Deductions[index] = deduction;
            NotifyChanged();
        }
    }

    public CalculationResult Recalculate()
    {
        var jurisdictions = string.IsNullOrWhiteSpace(HomeStateCode)
            ? JurisdictionContext.FederalOnly()
            : JurisdictionContext.FederalOnly() with { ResidentStateCode = HomeStateCode!.ToUpperInvariant() };

        var input = new PaycheckInput(
            ScenarioId, TaxYear, PayFrequency, WorkerType,
            Earnings.ToArray(), Deductions.ToArray(), W4, Household,
            jurisdictions, Ytd, RoundingPolicy, PayDate);

        var bundle = _rules.GetBundle(TaxYear);
        LatestResult = _calculator.Calculate(input, bundle);
        return LatestResult;
    }

    public void Reset()
    {
        ScenarioId = Guid.NewGuid();
        W4 = W4Profile.Default(FilingStatus.Single);
        Household = HouseholdTaxProfile.Default(FilingStatus.Single);
        Ytd = YtdSnapshot.Empty(TaxYear);
        Earnings.Clear();
        Earnings.Add(new EarningLineInput(EarningType.RegularSalary, Money.Usd(2500m)));
        Deductions.Clear();
        LatestResult = null;
        NotifyChanged();
    }

    private void NotifyChanged()
    {
        LatestResult = null;
        Changed?.Invoke();
    }
}
