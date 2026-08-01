namespace ExpenseManager.Core.Entities;

public class Transaction
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public DateTime Date { get; set; } = DateTime.Today;
    public string? Note { get; set; }

    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    public int WalletId { get; set; }
    public Wallet? Wallet { get; set; }

    /// <summary>Set when this transaction was auto-generated from a recurring schedule.</summary>
    public int? RecurringTransactionId { get; set; }
    public RecurringTransaction? RecurringTransaction { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
