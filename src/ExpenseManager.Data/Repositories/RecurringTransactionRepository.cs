using ExpenseManager.Core.Entities;
using ExpenseManager.Core.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ExpenseManager.Data.Repositories;

public class RecurringTransactionRepository(ExpenseManagerDbContext db) : IRecurringTransactionRepository
{
    public async Task<List<RecurringTransaction>> GetAllAsync(bool includeInactive = true, CancellationToken ct = default)
    {
        var query = db.RecurringTransactions.AsNoTracking()
            .Include(r => r.Category)
            .Include(r => r.Wallet)
            .AsQueryable();
        if (!includeInactive)
            query = query.Where(r => r.IsActive);
        return await query.OrderBy(r => r.NextDueDate).ToListAsync(ct);
    }

    public Task<RecurringTransaction?> GetByIdAsync(int id, CancellationToken ct = default) =>
        db.RecurringTransactions.Include(r => r.Category).Include(r => r.Wallet).FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<RecurringTransaction> AddAsync(RecurringTransaction recurring, CancellationToken ct = default)
    {
        db.RecurringTransactions.Add(recurring);
        await db.SaveChangesAsync(ct);
        return recurring;
    }

    public async Task UpdateAsync(RecurringTransaction recurring, CancellationToken ct = default)
    {
        db.RecurringTransactions.Update(recurring);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var entity = await db.RecurringTransactions.FindAsync([id], ct);
        if (entity is null) return;
        db.RecurringTransactions.Remove(entity);
        await db.SaveChangesAsync(ct);
    }

    public async Task<List<RecurringTransaction>> GetDueAsync(DateTime asOf, CancellationToken ct = default) =>
        await db.RecurringTransactions.AsNoTracking()
            .Include(r => r.Category)
            .Include(r => r.Wallet)
            .Where(r => r.IsActive && r.NextDueDate.Date <= asOf.Date && (r.EndDate == null || r.EndDate >= r.NextDueDate))
            .ToListAsync(ct);

    public async Task<List<RecurringTransaction>> GetUpcomingAsync(DateTime asOf, int days, CancellationToken ct = default)
    {
        var cutoff = asOf.Date.AddDays(days);
        return await db.RecurringTransactions.AsNoTracking()
            .Include(r => r.Category)
            .Include(r => r.Wallet)
            .Where(r => r.IsActive && r.NextDueDate.Date > asOf.Date && r.NextDueDate.Date <= cutoff)
            .OrderBy(r => r.NextDueDate)
            .ToListAsync(ct);
    }
}
