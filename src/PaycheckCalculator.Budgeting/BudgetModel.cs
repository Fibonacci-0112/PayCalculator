using PaycheckCalculator.Core.ValueObjects;

namespace PaycheckCalculator.Budgeting;

public enum BudgetMode { Envelope, ZeroBased, Traditional }
public enum BudgetPeriodType { Payday, Monthly, Annual }
public enum BudgetCategoryType { FixedExpense, VariableExpense, Savings, Debt, Discretionary, TaxReserve }

public sealed record BudgetPlan(Guid Id, string Name, BudgetMode Mode, BudgetPeriodType PeriodType, DateOnly StartDate);

public sealed record BudgetCategory(Guid Id, string Name, BudgetCategoryType Type, Money PlannedAmount, bool RolloverEnabled);

public sealed record BudgetTransaction(Guid Id, Guid? CategoryId, DateOnly TransactionDate, string Description, Money Amount);

public sealed class PaydayBudgetAllocator
{
    public IReadOnlyList<(BudgetCategory category, Money allocation)> Allocate(
        Money netPay, IReadOnlyList<BudgetCategory> categories)
    {
        var total = categories.Aggregate(Money.Zero, (acc, c) => acc + c.PlannedAmount);
        if (total.Amount <= 0m)
            return categories.Select(c => (c, Money.Zero)).ToArray();

        var allocations = new List<(BudgetCategory, Money)>();
        foreach (var c in categories)
        {
            var share = netPay * (c.PlannedAmount.Amount / total.Amount);
            allocations.Add((c, share));
        }
        return allocations;
    }
}
