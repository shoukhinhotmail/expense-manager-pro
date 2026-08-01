namespace ExpenseManager.Core.Entities;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>Segoe Fluent Icons glyph code (e.g. "") used as the category icon.</summary>
    public string Glyph { get; set; } = "";

    /// <summary>Hex color, e.g. "#FF6B6B".</summary>
    public string Color { get; set; } = "#6B7280";

    public TransactionType Type { get; set; } = TransactionType.Expense;

    public bool IsDefault { get; set; }

    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
