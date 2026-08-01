using ExpenseManager.Core.Entities;
using ExpenseManager.Core.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ExpenseManager.Data.Repositories;

public class CategoryRepository(ExpenseManagerDbContext db) : ICategoryRepository
{
    public async Task<List<Category>> GetAllAsync(TransactionType? type = null, CancellationToken ct = default)
    {
        var query = db.Categories.AsNoTracking().AsQueryable();
        if (type is not null)
            query = query.Where(c => c.Type == type);
        return await query.OrderBy(c => c.Name).ToListAsync(ct);
    }

    public Task<Category?> GetByIdAsync(int id, CancellationToken ct = default) =>
        db.Categories.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<Category> AddAsync(Category category, CancellationToken ct = default)
    {
        db.Categories.Add(category);
        await db.SaveChangesAsync(ct);
        return category;
    }

    public async Task UpdateAsync(Category category, CancellationToken ct = default)
    {
        db.Categories.Update(category);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var entity = await db.Categories.FindAsync([id], ct);
        if (entity is null) return;
        db.Categories.Remove(entity);
        await db.SaveChangesAsync(ct);
    }
}
