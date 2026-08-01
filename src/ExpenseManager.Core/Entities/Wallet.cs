namespace ExpenseManager.Core.Entities;

public class Wallet
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public WalletType Type { get; set; } = WalletType.Cash;
    public string Color { get; set; } = "#6366F1";

    /// <summary>Balance before any tracked transactions — lets a wallet start mid-history.</summary>
    public decimal InitialBalance { get; set; }

    public bool IsDefault { get; set; }
    public bool IsArchived { get; set; }

    /// <summary>True for the starter wallets created on first run. These can be renamed or
    /// recolored but not deleted, so the app always has at least one wallet to post to.</summary>
    public bool IsSystem { get; set; }

    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
