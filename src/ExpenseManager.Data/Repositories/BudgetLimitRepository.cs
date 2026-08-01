using ExpenseManager.Core.Entities;
using ExpenseManager.Core.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ExpenseManager.Data.Repositories;

public class BudgetLimitRepository(ExpenseManagerDbContext db) : IBudgetLimitRepository
{
    public async Task<List<BudgetLimit>> GetAllAsync(bool includeArchived = false, CancellationToken ct = default)
    {
        var query = db.BudgetLimits.AsNoTracking().Include(b => b.Category).AsQueryable();
        if (!includeArchived)
            query = query.Where(b => !b.IsArchived);
        return await query.OrderBy(b => b.Category!.Name).ToListAsync(ct);
    }

    public Task<BudgetLimit?> GetByIdAsync(int id, CancellationToken ct = default) =>
        db.BudgetLimits.Include(b => b.Category).FirstOrDefaultAsync(b => b.Id == id, ct);

    public async Task<BudgetLimit> AddAsync(BudgetLimit budget, CancellationToken ct = default)
    {
        db.BudgetLimits.Add(budget);
        await db.SaveChangesAsync(ct);
        return budget;
    }

    public async Task UpdateAsync(BudgetLimit budget, CancellationToken ct = default)
    {
        db.BudgetLimits.Update(budget);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var entity = await db.BudgetLimits.FindAsync([id], ct);
        if (entity is null) return;
        db.BudgetLimits.Remove(entity);
        await db.SaveChangesAsync(ct);
    }

    public async Task<decimal> GetSpentInCurrentPeriodAsync(int budgetLimitId, CancellationToken ct = default)
    {
        var budget = await db.BudgetLimits.AsNoTracking().FirstOrDefaultAsync(b => b.Id == budgetLimitId, ct);
        if (budget is null) return 0m;

        var periodStart = GetPeriodStart(budget.Period);
        return await db.Transactions.AsNoTracking()
            .Where(t => t.Type == TransactionType.Expense && t.CategoryId == budget.CategoryId && t.Date >= periodStart)
            .SumAsync(t => (decimal?)t.Amount, ct) ?? 0m;
    }

    private static DateTime GetPeriodStart(BudgetPeriod period)
    {
        var today = DateTime.Today;
        if (period == BudgetPeriod.Weekly)
        {
            // Monday-start week, matching how most budgeting apps define a "weekly" cadence.
            var diff = ((int)today.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
            return today.AddDays(-diff);
        }
        return new DateTime(today.Year, today.Month, 1);
    }
}
