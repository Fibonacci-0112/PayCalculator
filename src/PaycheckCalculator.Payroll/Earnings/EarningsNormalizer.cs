using PaycheckCalculator.Core.Earnings;
using PaycheckCalculator.Core.ValueObjects;

namespace PaycheckCalculator.Payroll.Earnings;

public sealed record NormalizedEarning(
    EarningType Type,
    Money Amount,
    bool IsSupplemental,
    TaxTreatmentCode TaxTreatment);

public sealed class EarningsNormalizer
{
    public IReadOnlyList<NormalizedEarning> Normalize(IEnumerable<EarningLineInput> lines)
    {
        var result = new List<NormalizedEarning>();
        foreach (var line in lines)
        {
            var amount = line.Amount;
            if (line.Hours is decimal hours && line.Rate is Money rate && amount.Amount == 0m)
            {
                amount = rate * hours;
            }
            var isSupplemental = line.IsSupplemental || line.Type is
                EarningType.Bonus or EarningType.Commission or EarningType.PtoPayout;
            result.Add(new NormalizedEarning(line.Type, amount, isSupplemental, line.TaxTreatment));
        }
        return result;
    }
}
