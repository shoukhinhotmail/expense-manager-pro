using ExpenseManager.Core.Entities;
using ExpenseManager.Core.Repositories;

namespace ExpenseManager.App.Services;

public class RecurringService(IRecurringTransactionRepository recurringRepository, ITransactionRepository transactionRepository)
{
    /// <summary>Posts a Transaction for every occurrence up to today for each due schedule, advancing
    /// NextDueDate past today. Caps catch-up at 60 occurrences per schedule so a stale StartDate can't
    /// generate an unbounded flood.</summary>
    /// <returns>One summary line per schedule that generated at least one transaction.</returns>
    public async Task<List<string>> ProcessDueAsync(CancellationToken ct = default)
    {
        var today = DateTime.Today;
        var due = await recurringRepository.GetDueAsync(today, ct);
        var summaries = new List<string>();

        foreach (var recurring in due)
        {
            var count = 0;
            while (recurring.NextDueDate.Date <= today && count < 60)
            {
                if (recurring.EndDate is not null && recurring.NextDueDate.Date > recurring.EndDate.Value.Date)
                    break;

                await transactionRepository.AddAsync(new Transaction
                {
                    Amount = recurring.Amount,
                    Type = recurring.Type,
                    Date = recurring.NextDueDate.Date,
                    Note = recurring.Note,
                    CategoryId = recurring.CategoryId,
                    WalletId = recurring.WalletId,
                    RecurringTransactionId = recurring.Id
                }, ct);

                recurring.NextDueDate = RecurringTransaction.Advance(recurring.NextDueDate, recurring.Frequency);
                count++;
            }

            if (count > 0)
            {
                if (recurring.EndDate is not null && recurring.NextDueDate.Date > recurring.EndDate.Value.Date)
                    recurring.IsActive = false;

                await recurringRepository.UpdateAsync(recurring, ct);
                var categoryName = recurring.Category?.Name ?? "Uncategorized";
                summaries.Add(count == 1
                    ? $"{categoryName} — {recurring.Amount:0.##}"
                    : $"{categoryName} — {recurring.Amount:0.##} x{count}");
            }
        }

        return summaries;
    }

    public Task<List<RecurringTransaction>> GetUpcomingAsync(int withinDays = 3, CancellationToken ct = default) =>
        recurringRepository.GetUpcomingAsync(DateTime.Today, withinDays, ct);
}
