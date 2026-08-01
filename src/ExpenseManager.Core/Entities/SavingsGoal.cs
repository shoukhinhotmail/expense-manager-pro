namespace ExpenseManager.Core.Entities;

public class SavingsGoal
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal TargetAmount { get; set; }

    /// <summary>Manually updated via contributions — this feature does not link to a wallet
    /// balance, since a wallet may be used for other spending unrelated to this goal.</summary>
    public decimal CurrentAmount { get; set; }

    public DateTime? TargetDate { get; set; }

    /// <summary>Hex color, e.g. "#22C55E" — used for the progress bar and icon tint.</summary>
    public string Color { get; set; } = "#22C55E";

    public bool IsArchived { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
