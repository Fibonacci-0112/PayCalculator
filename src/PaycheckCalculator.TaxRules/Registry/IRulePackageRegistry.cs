using PaycheckCalculator.Core.ValueObjects;
using PaycheckCalculator.TaxRules.Federal2026;
using PaycheckCalculator.TaxRules.Model;

namespace PaycheckCalculator.TaxRules.Registry;

public interface IRulePackageRegistry
{
    TaxRuleSetBundle GetBundle(TaxYear year);
    TaxRuleSet GetFederalRuleSet(TaxYear year);
}

public sealed class InMemoryRulePackageRegistry : IRulePackageRegistry
{
    private readonly Dictionary<int, TaxRuleSetBundle> _bundles = new();

    public InMemoryRulePackageRegistry()
    {
        _bundles[2026] = new TaxRuleSetBundle(
            TaxYear: new TaxYear(2026),
            Federal: FederalRule2026.ToRuleSet(),
            States: new Dictionary<string, TaxRuleSet>(),
            Locals: new Dictionary<string, TaxRuleSet>(),
            RetirementLimits: null,
            HsaFsaLimits: null);
    }

    public TaxRuleSetBundle GetBundle(TaxYear year)
    {
        if (!_bundles.TryGetValue(year.Year, out var bundle))
        {
            throw new InvalidOperationException(
                $"No tax rule bundle is installed for tax year {year.Year}.");
        }
        return bundle;
    }

    public TaxRuleSet GetFederalRuleSet(TaxYear year) => GetBundle(year).Federal;
}
