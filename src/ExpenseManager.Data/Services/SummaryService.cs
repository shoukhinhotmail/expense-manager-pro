using ExpenseManager.Core.Entities;
using ExpenseManager.Core.Models;
using ExpenseManager.Core.Services;
using Microsoft.EntityFrameworkCore;

namespace ExpenseManager.Data.Services;

public class SummaryService(ExpenseManagerDbContext db) : ISummaryService
{
    public async Task<DashboardSummary> GetSummaryAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        var transactions = await db.Transactions
            .AsNoTracking()
            .Include(t => t.Category)
            .Where(t => t.Date >= from && t.Date <= to)
            .ToListAsync(ct);

        var summary = new DashboardSummary
        {
            TotalIncome = transactions.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount),
            TotalExpense = transactions.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount)
        };

        summary.ExpenseByCategory = transactions
            .Where(t => t.Type == TransactionType.Expense && t.Category is not null)
            .GroupBy(t => t.CategoryId)
            .Select(g => new CategoryTotal
            {
                CategoryId = g.Key,
                CategoryName = g.First().Category!.Name,
                Color = g.First().Category!.Color,
                Total = g.Sum(t => t.Amount)
            })
            .OrderByDescending(c => c.Total)
            .ToList();

        return summary;
    }
}
