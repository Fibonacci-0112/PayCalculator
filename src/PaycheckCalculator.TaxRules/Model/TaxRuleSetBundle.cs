using PaycheckCalculator.Core.ValueObjects;

namespace PaycheckCalculator.TaxRules.Model;

public sealed record TaxRuleSetBundle(
    TaxYear TaxYear,
    TaxRuleSet Federal,
    IReadOnlyDictionary<string, TaxRuleSet> States,
    IReadOnlyDictionary<string, StateWithholdingRule> StateWithholding,
    IReadOnlyDictionary<string, TaxRuleSet> Locals,
    TaxRuleSet? RetirementLimits,
    TaxRuleSet? HsaFsaLimits)
{
    public IEnumerable<string> AllRuleSetVersions() =>
        new[] { Federal.RuleSetVersion }
            .Concat(States.Values.Select(s => s.RuleSetVersion))
            .Concat(Locals.Values.Select(l => l.RuleSetVersion))
            .Concat(RetirementLimits is null ? Array.Empty<string>() : new[] { RetirementLimits.RuleSetVersion })
            .Concat(HsaFsaLimits is null ? Array.Empty<string>() : new[] { HsaFsaLimits.RuleSetVersion });
}
