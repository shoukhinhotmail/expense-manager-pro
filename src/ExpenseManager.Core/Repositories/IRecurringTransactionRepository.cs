using ExpenseManager.Core.Entities;

namespace ExpenseManager.Core.Repositories;

public interface IRecurringTransactionRepository
{
    Task<List<RecurringTransaction>> GetAllAsync(bool includeInactive = true, CancellationToken ct = default);
    Task<RecurringTransaction?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<RecurringTransaction> AddAsync(RecurringTransaction recurring, CancellationToken ct = default);
    Task UpdateAsync(RecurringTransaction recurring, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);

    /// <summary>Active schedules whose NextDueDate is on or before <paramref name="asOf"/>.</summary>
    Task<List<RecurringTransaction>> GetDueAsync(DateTime asOf, CancellationToken ct = default);

    /// <summary>Active schedules whose NextDueDate falls within the next <paramref name="days"/> days
    /// (inclusive), for surfacing "coming up" reminders.</summary>
    Task<List<RecurringTransaction>> GetUpcomingAsync(DateTime asOf, int days, CancellationToken ct = default);
}
