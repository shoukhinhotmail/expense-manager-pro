using ExpenseManager.Core.Entities;
using ExpenseManager.Data;
using ExpenseManager.Data.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ExpenseManager.Tests;

public class SummaryServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ExpenseManagerDbContext _db;

    public SummaryServiceTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ExpenseManagerDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new ExpenseManagerDbContext(options);
        _db.Database.EnsureCreated();
    }

    [Fact]
    public async Task GetSummaryAsync_AggregatesIncomeAndExpenseSeparately()
    {
        var groceries = await _db.Categories.FirstAsync(c => c.Name == "Groceries");
        var salary = await _db.Categories.FirstAsync(c => c.Name == "Salary");
        var wallet = await _db.Wallets.FirstAsync();
        var today = DateTime.Today;

        _db.Transactions.AddRange(
            new Transaction { Amount = 50m, Type = TransactionType.Expense, CategoryId = groceries.Id, WalletId = wallet.Id, Date = today },
            new Transaction { Amount = 30m, Type = TransactionType.Expense, CategoryId = groceries.Id, WalletId = wallet.Id, Date = today },
            new Transaction { Amount = 1000m, Type = TransactionType.Income, CategoryId = salary.Id, WalletId = wallet.Id, Date = today }
        );
        await _db.SaveChangesAsync();

        var service = new SummaryService(_db);
        var summary = await service.GetSummaryAsync(today.AddDays(-1), today.AddDays(1));

        Assert.Equal(80m, summary.TotalExpense);
        Assert.Equal(1000m, summary.TotalIncome);
        Assert.Equal(920m, summary.Balance);

        var groceriesTotal = Assert.Single(summary.ExpenseByCategory);
        Assert.Equal("Groceries", groceriesTotal.CategoryName);
        Assert.Equal(80m, groceriesTotal.Total);
    }

    [Fact]
    public async Task GetSummaryAsync_ExcludesTransactionsOutsideDateRange()
    {
        var groceries = await _db.Categories.FirstAsync(c => c.Name == "Groceries");
        var wallet = await _db.Wallets.FirstAsync();
        var today = DateTime.Today;

        _db.Transactions.Add(new Transaction
        {
            Amount = 999m,
            Type = TransactionType.Expense,
            CategoryId = groceries.Id,
            WalletId = wallet.Id,
            Date = today.AddYears(-1)
        });
        await _db.SaveChangesAsync();

        var service = new SummaryService(_db);
        var summary = await service.GetSummaryAsync(today.AddDays(-7), today);

        Assert.Equal(0m, summary.TotalExpense);
        Assert.Empty(summary.ExpenseByCategory);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}
