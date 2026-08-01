using ExpenseManager.Core.Entities;
using ExpenseManager.Core.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ExpenseManager.Data.Repositories;

public class TransactionRepository(ExpenseManagerDbContext db) : ITransactionRepository
{
    public async Task<List<Transaction>> GetAllAsync(
        TransactionType? type = null,
        DateTime? from = null,
        DateTime? to = null,
        int? categoryId = null,
        int? walletId = null,
        string? searchText = null,
        decimal? minAmount = null,
        decimal? maxAmount = null,
        CancellationToken ct = default)
    {
        var query = db.Transactions.AsNoTracking()
            .Include(t => t.Category)
            .Include(t => t.Wallet)
            .AsQueryable();

        if (type is not null)
            query = query.Where(t => t.Type == type);
        if (from is not null)
            query = query.Where(t => t.Date >= from.Value);
        if (to is not null)
            query = query.Where(t => t.Date <= to.Value);
        if (categoryId is not null)
            query = query.Where(t => t.CategoryId == categoryId);
        if (walletId is not null)
            query = query.Where(t => t.WalletId == walletId);
        if (!string.IsNullOrWhiteSpace(searchText))
            query = query.Where(t => t.Note != null && t.Note.Contains(searchText));
        if (minAmount is not null)
            query = query.Where(t => t.Amount >= minAmount.Value);
        if (maxAmount is not null)
            query = query.Where(t => t.Amount <= maxAmount.Value);

        return await query.OrderByDescending(t => t.Date).ThenByDescending(t => t.Id).ToListAsync(ct);
    }

    public Task<Transaction?> GetByIdAsync(int id, CancellationToken ct = default) =>
        db.Transactions.Include(t => t.Category).Include(t => t.Wallet).FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<Transaction> AddAsync(Transaction transaction, CancellationToken ct = default)
    {
        db.Transactions.Add(transaction);
        await db.SaveChangesAsync(ct);
        return transaction;
    }

    public async Task UpdateAsync(Transaction transaction, CancellationToken ct = default)
    {
        transaction.UpdatedAt = DateTime.UtcNow;
        db.Transactions.Update(transaction);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var entity = await db.Transactions.FindAsync([id], ct);
        if (entity is null) return;
        db.Transactions.Remove(entity);
        await db.SaveChangesAsync(ct);
    }

    public Task<int> CountByRecurringTransactionIdAsync(int recurringTransactionId, CancellationToken ct = default) =>
        db.Transactions.CountAsync(t => t.RecurringTransactionId == recurringTransactionId, ct);

    public async Task DeleteByRecurringTransactionIdAsync(int recurringTransactionId, CancellationToken ct = default)
    {
        var entities = await db.Transactions.Where(t => t.RecurringTransactionId == recurringTransactionId).ToListAsync(ct);
        db.Transactions.RemoveRange(entities);
        await db.SaveChangesAsync(ct);
    }
}
