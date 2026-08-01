using ExpenseManager.Core.Entities;

namespace ExpenseManager.Core.Repositories;

public interface IBudgetLimitRepository
{
    Task<List<BudgetLimit>> GetAllAsync(bool includeArchived = false, CancellationToken ct = default);
    Task<BudgetLimit?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<BudgetLimit> AddAsync(BudgetLimit budget, CancellationToken ct = default);
    Task UpdateAsync(BudgetLimit budget, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);

    /// <summary>Sum of expense transactions for the budget's category within its current
    /// period (this week or this month, depending on <see cref="BudgetLimit.Period"/>).</summary>
    Task<decimal> GetSpentInCurrentPeriodAsync(int budgetLimitId, CancellationToken ct = default);
}
