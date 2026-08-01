using ExpenseManager.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExpenseManager.Data;

public class ExpenseManagerDbContext(DbContextOptions<ExpenseManagerDbContext> options) : DbContext(options)
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<RecurringTransaction> RecurringTransactions => Set<RecurringTransaction>();
    public DbSet<SavingsGoal> SavingsGoals => Set<SavingsGoal>();
    public DbSet<BudgetLimit> BudgetLimits => Set<BudgetLimit>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>(entity =>
        {
            entity.Property(c => c.Name).IsRequired().HasMaxLength(100);
            entity.Property(c => c.Color).IsRequired().HasMaxLength(20);
        });

        modelBuilder.Entity<Wallet>(entity =>
        {
            entity.Property(w => w.Name).IsRequired().HasMaxLength(100);
            entity.Property(w => w.Color).IsRequired().HasMaxLength(20);
            entity.Property(w => w.InitialBalance).HasColumnType("TEXT");
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.Property(t => t.Amount).HasColumnType("TEXT");
            entity.HasOne(t => t.Category)
                  .WithMany(c => c.Transactions)
                  .HasForeignKey(t => t.CategoryId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(t => t.Wallet)
                  .WithMany(w => w.Transactions)
                  .HasForeignKey(t => t.WalletId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(t => t.RecurringTransaction)
                  .WithMany()
                  .HasForeignKey(t => t.RecurringTransactionId)
                  .OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(t => t.Date);
            entity.HasIndex(t => t.Type);
            entity.HasIndex(t => t.WalletId);
        });

        modelBuilder.Entity<RecurringTransaction>(entity =>
        {
            entity.Property(r => r.Amount).HasColumnType("TEXT");
            entity.HasOne(r => r.Category)
                  .WithMany()
                  .HasForeignKey(r => r.CategoryId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(r => r.Wallet)
                  .WithMany()
                  .HasForeignKey(r => r.WalletId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(r => r.NextDueDate);
        });

        modelBuilder.Entity<SavingsGoal>(entity =>
        {
            entity.Property(g => g.Name).IsRequired().HasMaxLength(100);
            entity.Property(g => g.TargetAmount).HasColumnType("TEXT");
            entity.Property(g => g.CurrentAmount).HasColumnType("TEXT");
            entity.Property(g => g.Color).IsRequired().HasMaxLength(20);
        });

        modelBuilder.Entity<BudgetLimit>(entity =>
        {
            entity.Property(b => b.LimitAmount).HasColumnType("TEXT");
            entity.HasOne(b => b.Category)
                  .WithMany()
                  .HasForeignKey(b => b.CategoryId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        SeedData.Apply(modelBuilder);
    }
}
