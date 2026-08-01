using ExpenseManager.Core.Entities;
using ExpenseManager.Core.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ExpenseManager.Data.Repositories;

public class SavingsGoalRepository(ExpenseManagerDbContext db) : ISavingsGoalRepository
{
    public async Task<List<SavingsGoal>> GetAllAsync(bool includeArchived = false, CancellationToken ct = default)
    {
        var query = db.SavingsGoals.AsNoTracking().AsQueryable();
        if (!includeArchived)
            query = query.Where(g => !g.IsArchived);
        return await query.OrderBy(g => g.TargetDate ?? DateTime.MaxValue).ThenBy(g => g.Name).ToListAsync(ct);
    }

    public Task<SavingsGoal?> GetByIdAsync(int id, CancellationToken ct = default) =>
        db.SavingsGoals.FirstOrDefaultAsync(g => g.Id == id, ct);

    public async Task<SavingsGoal> AddAsync(SavingsGoal goal, CancellationToken ct = default)
    {
        db.SavingsGoals.Add(goal);
        await db.SaveChangesAsync(ct);
        return goal;
    }

    public async Task UpdateAsync(SavingsGoal goal, CancellationToken ct = default)
    {
        db.SavingsGoals.Update(goal);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var entity = await db.SavingsGoals.FindAsync([id], ct);
        if (entity is null) return;
        db.SavingsGoals.Remove(entity);
        await db.SaveChangesAsync(ct);
    }

    public async Task<SavingsGoal> AddContributionAsync(int id, decimal amount, CancellationToken ct = default)
    {
        var entity = await db.SavingsGoals.FindAsync([id], ct)
            ?? throw new InvalidOperationException($"Savings goal {id} not found.");
        entity.CurrentAmount += amount;
        await db.SaveChangesAsync(ct);
        return entity;
    }
}
