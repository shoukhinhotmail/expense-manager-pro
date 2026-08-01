using ExpenseManager.Core.Entities;

namespace ExpenseManager.Core.Repositories;

public interface ITransactionRepository
{
    Task<List<Transaction>> GetAllAsync(
        TransactionType? type = null,
        DateTime? from = null,
        DateTime? to = null,
        int? categoryId = null,
        int? walletId = null,
        string? searchText = null,
        decimal? minAmount = null,
        decimal? maxAmount = null,
        CancellationToken ct = default);

    Task<Transaction?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Transaction> AddAsync(Transaction transaction, CancellationToken ct = default);
    Task UpdateAsync(Transaction transaction, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);

    Task<int> CountByRecurringTransactionIdAsync(int recurringTransactionId, CancellationToken ct = default);
    Task DeleteByRecurringTransactionIdAsync(int recurringTransactionId, CancellationToken ct = default);
}
