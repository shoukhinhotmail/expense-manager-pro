using ExpenseManager.Core.Entities;

namespace ExpenseManager.Core.Repositories;

public interface ISavingsGoalRepository
{
    Task<List<SavingsGoal>> GetAllAsync(bool includeArchived = false, CancellationToken ct = default);
    Task<SavingsGoal?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<SavingsGoal> AddAsync(SavingsGoal goal, CancellationToken ct = default);
    Task UpdateAsync(SavingsGoal goal, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);

    /// <summary>Adds (or, with a negative amount, removes) a contribution and returns the
    /// updated goal.</summary>
    Task<SavingsGoal> AddContributionAsync(int id, decimal amount, CancellationToken ct = default);
}
