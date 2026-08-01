namespace ExpenseManager.Core.Entities;

/// <summary>A spending cap on one category for a recurring period (e.g. "Groceries, $400/month").
/// Progress is computed from existing expense transactions, not stored — there is nothing to
/// keep in sync when transactions are added, edited, or deleted.</summary>
public class BudgetLimit
{
    public int Id { get; set; }

    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    public decimal LimitAmount { get; set; }
    public BudgetPeriod Period { get; set; } = BudgetPeriod.Monthly;

    public bool IsArchived { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
