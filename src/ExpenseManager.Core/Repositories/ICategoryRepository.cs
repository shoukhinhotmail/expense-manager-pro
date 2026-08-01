using ExpenseManager.Core.Entities;

namespace ExpenseManager.Core.Repositories;

public interface ICategoryRepository
{
    Task<List<Category>> GetAllAsync(TransactionType? type = null, CancellationToken ct = default);
    Task<Category?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Category> AddAsync(Category category, CancellationToken ct = default);
    Task UpdateAsync(Category category, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
