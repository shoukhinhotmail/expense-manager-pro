using ExpenseManager.Core.Models;

namespace ExpenseManager.Core.Services;

public interface ISummaryService
{
    Task<DashboardSummary> GetSummaryAsync(DateTime from, DateTime to, CancellationToken ct = default);
}
