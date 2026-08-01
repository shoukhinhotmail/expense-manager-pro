using ExpenseManager.Core.Entities;

namespace ExpenseManager.Core.Repositories;

public interface IWalletRepository
{
    Task<List<Wallet>> GetAllAsync(bool includeArchived = false, CancellationToken ct = default);
    Task<Wallet?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Wallet> AddAsync(Wallet wallet, CancellationToken ct = default);
    Task UpdateAsync(Wallet wallet, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);

    /// <summary>InitialBalance plus the net of every transaction posted to this wallet.</summary>
    Task<decimal> GetCurrentBalanceAsync(int walletId, CancellationToken ct = default);
}
