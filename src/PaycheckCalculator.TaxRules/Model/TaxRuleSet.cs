using PaycheckCalculator.Core.ValueObjects;

namespace PaycheckCalculator.TaxRules.Model;

public sealed record TaxRuleSet(
    string RuleSetId,
    TaxYear TaxYear,
    string JurisdictionCode,
    string RuleSetVersion,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    string SourceDocumentName,
    string? SourceDocumentUri,
    string SourceRevision,
    string EngineMinVersion,
    IReadOnlyList<TaxTable> Tables,
    IReadOnlyList<FormulaRule> Formulas,
    IReadOnlyList<DeductionRule> Deductions,
    IReadOnlyList<CreditRule> Credits,
    IReadOnlyList<ValidationCase> ValidationCases,
    string? PackageSignature);

public sealed record TaxTable(string TableId, string Description, IReadOnlyList<TaxBracket> Brackets);

public sealed record TaxBracket(decimal Floor, decimal? Ceiling, decimal BaseTax, decimal MarginalRate);

public sealed record FormulaRule(string FormulaId, string Description, string Expression);

public sealed record DeductionRule(string DeductionId, string Description, decimal? AnnualLimit);

public sealed record CreditRule(string CreditId, string Description, decimal MaxAmount);

public sealed record ValidationCase(string CaseId, string Description);
