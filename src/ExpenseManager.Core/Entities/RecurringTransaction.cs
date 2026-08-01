namespace ExpenseManager.Core.Entities;

public class RecurringTransaction
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public string? Note { get; set; }

    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    public int WalletId { get; set; }
    public Wallet? Wallet { get; set; }

    public RecurrenceFrequency Frequency { get; set; } = RecurrenceFrequency.Monthly;
    public DateTime StartDate { get; set; } = DateTime.Today;
    public DateTime? EndDate { get; set; }

    /// <summary>The next occurrence still to be posted. Advances each time a transaction is generated.</summary>
    public DateTime NextDueDate { get; set; } = DateTime.Today;

    public bool IsActive { get; set; } = true;

    /// <summary>How many days before NextDueDate to surface a reminder notification. 0 = due-day only.</summary>
    public int ReminderDaysBefore { get; set; } = 1;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public static DateTime Advance(DateTime date, RecurrenceFrequency frequency) => frequency switch
    {
        RecurrenceFrequency.Daily => date.AddDays(1),
        RecurrenceFrequency.Weekly => date.AddDays(7),
        RecurrenceFrequency.Biweekly => date.AddDays(14),
        RecurrenceFrequency.Monthly => date.AddMonths(1),
        RecurrenceFrequency.Yearly => date.AddYears(1),
        _ => date.AddMonths(1)
    };
}
