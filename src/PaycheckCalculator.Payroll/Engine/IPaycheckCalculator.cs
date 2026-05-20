using PaycheckCalculator.Core.Calculations;
using PaycheckCalculator.TaxRules.Model;

namespace PaycheckCalculator.Payroll.Engine;

public interface IPaycheckCalculator
{
    CalculationResult Calculate(PaycheckInput input, TaxRuleSetBundle rules);
}
