using ExpenseManager.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExpenseManager.Data;

internal static class SeedData
{
    public static void Apply(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Food & Dining", Glyph = "", Color = "#F97316", Type = TransactionType.Expense, IsDefault = true },
            new Category { Id = 2, Name = "Groceries", Glyph = "", Color = "#22C55E", Type = TransactionType.Expense, IsDefault = true },
            new Category { Id = 3, Name = "Transport", Glyph = "", Color = "#3B82F6", Type = TransactionType.Expense, IsDefault = true },
            new Category { Id = 4, Name = "Shopping", Glyph = "", Color = "#EC4899", Type = TransactionType.Expense, IsDefault = true },
            new Category { Id = 5, Name = "Bills & Utilities", Glyph = "", Color = "#EF4444", Type = TransactionType.Expense, IsDefault = true },
            new Category { Id = 6, Name = "Entertainment", Glyph = "", Color = "#A855F7", Type = TransactionType.Expense, IsDefault = true },
            new Category { Id = 7, Name = "Health", Glyph = "", Color = "#14B8A6", Type = TransactionType.Expense, IsDefault = true },
            new Category { Id = 8, Name = "Other", Glyph = "", Color = "#6B7280", Type = TransactionType.Expense, IsDefault = true },
            new Category { Id = 9, Name = "Salary", Glyph = "", Color = "#22C55E", Type = TransactionType.Income, IsDefault = true },
            new Category { Id = 10, Name = "Freelance", Glyph = "", Color = "#3B82F6", Type = TransactionType.Income, IsDefault = true },
            new Category { Id = 11, Name = "Investments", Glyph = "", Color = "#F59E0B", Type = TransactionType.Income, IsDefault = true },
            new Category { Id = 12, Name = "Other Income", Glyph = "", Color = "#6B7280", Type = TransactionType.Income, IsDefault = true }
        );

        modelBuilder.Entity<Wallet>().HasData(
            new Wallet { Id = 1, Name = "Cash", Type = WalletType.Cash, Color = "#22C55E", IsDefault = true, IsSystem = true },
            new Wallet { Id = 2, Name = "Bank Account", Type = WalletType.Bank, Color = "#3B82F6", IsDefault = false, IsSystem = true },
            new Wallet { Id = 3, Name = "Credit Card", Type = WalletType.CreditCard, Color = "#A855F7", IsDefault = false, IsSystem = true },
            new Wallet { Id = 4, Name = "Mobile Banking", Type = WalletType.MobileBanking, Color = "#F97316", IsDefault = false, IsSystem = true }
        );
    }
}
