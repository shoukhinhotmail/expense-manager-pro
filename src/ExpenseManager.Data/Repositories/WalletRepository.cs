using ExpenseManager.Core.Entities;
using ExpenseManager.Core.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ExpenseManager.Data.Repositories;

public class WalletRepository(ExpenseManagerDbContext db) : IWalletRepository
{
    public async Task<List<Wallet>> GetAllAsync(bool includeArchived = false, CancellationToken ct = default)
    {
        var query = db.Wallets.AsNoTracking().AsQueryable();
        if (!includeArchived)
            query = query.Where(w => !w.IsArchived);
        return await query.OrderBy(w => w.Name).ToListAsync(ct);
    }

    public Task<Wallet?> GetByIdAsync(int id, CancellationToken ct = default) =>
        db.Wallets.FirstOrDefaultAsync(w => w.Id == id, ct);

    public async Task<Wallet> AddAsync(Wallet wallet, CancellationToken ct = default)
    {
        db.Wallets.Add(wallet);
        await db.SaveChangesAsync(ct);
        return wallet;
    }

    public async Task UpdateAsync(Wallet wallet, CancellationToken ct = default)
    {
        db.Wallets.Update(wallet);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var entity = await db.Wallets.FindAsync([id], ct);
        if (entity is null) return;
        db.Wallets.Remove(entity);
        await db.SaveChangesAsync(ct);
    }

    public async Task<decimal> GetCurrentBalanceAsync(int walletId, CancellationToken ct = default)
    {
        var wallet = await db.Wallets.AsNoTracking().FirstOrDefaultAsync(w => w.Id == walletId, ct);
        if (wallet is null) return 0m;

        var income = await db.Transactions.AsNoTracking()
            .Where(t => t.WalletId == walletId && t.Type == TransactionType.Income)
            .SumAsync(t => t.Amount, ct);
        var expense = await db.Transactions.AsNoTracking()
            .Where(t => t.WalletId == walletId && t.Type == TransactionType.Expense)
            .SumAsync(t => t.Amount, ct);

        return wallet.InitialBalance + income - expense;
    }
}
