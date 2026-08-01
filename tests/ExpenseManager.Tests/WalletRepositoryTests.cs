using ExpenseManager.Core.Entities;
using ExpenseManager.Data;
using ExpenseManager.Data.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ExpenseManager.Tests;

public class WalletRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ExpenseManagerDbContext _db;
    private readonly WalletRepository _repository;

    public WalletRepositoryTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ExpenseManagerDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new ExpenseManagerDbContext(options);
        _db.Database.EnsureCreated();
        _repository = new WalletRepository(_db);
    }

    [Fact]
    public async Task GetCurrentBalanceAsync_AddsInitialBalancePlusIncomeMinusExpense()
    {
        var wallet = await _repository.AddAsync(new Wallet { Name = "Test Wallet", Type = WalletType.Cash, InitialBalance = 100m });
        var category = await _db.Categories.FirstAsync(c => c.Type == TransactionType.Expense);
        var incomeCategory = await _db.Categories.FirstAsync(c => c.Type == TransactionType.Income);

        _db.Transactions.AddRange(
            new Transaction { Amount = 300m, Type = TransactionType.Income, CategoryId = incomeCategory.Id, WalletId = wallet.Id, Date = DateTime.Today },
            new Transaction { Amount = 75m, Type = TransactionType.Expense, CategoryId = category.Id, WalletId = wallet.Id, Date = DateTime.Today }
        );
        await _db.SaveChangesAsync();

        var balance = await _repository.GetCurrentBalanceAsync(wallet.Id);

        Assert.Equal(325m, balance); // 100 + 300 - 75
    }

    [Fact]
    public async Task GetCurrentBalanceAsync_IgnoresOtherWallets()
    {
        var walletA = await _repository.AddAsync(new Wallet { Name = "A", Type = WalletType.Cash, InitialBalance = 0m });
        var walletB = await _repository.AddAsync(new Wallet { Name = "B", Type = WalletType.Bank, InitialBalance = 0m });
        var category = await _db.Categories.FirstAsync(c => c.Type == TransactionType.Expense);

        _db.Transactions.Add(new Transaction { Amount = 500m, Type = TransactionType.Expense, CategoryId = category.Id, WalletId = walletB.Id, Date = DateTime.Today });
        await _db.SaveChangesAsync();

        var balanceA = await _repository.GetCurrentBalanceAsync(walletA.Id);

        Assert.Equal(0m, balanceA);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}
