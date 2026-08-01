using ExpenseManager.Core.Entities;
using ExpenseManager.Data;
using ExpenseManager.Data.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ExpenseManager.Tests;

public class TransactionRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ExpenseManagerDbContext _db;
    private readonly TransactionRepository _repository;
    private readonly int _walletId;

    public TransactionRepositoryTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ExpenseManagerDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new ExpenseManagerDbContext(options);
        _db.Database.EnsureCreated();
        _repository = new TransactionRepository(_db);
        _walletId = _db.Wallets.First().Id;
    }

    [Fact]
    public async Task AddAsync_ThenGetById_ReturnsSameTransaction()
    {
        var category = await _db.Categories.FirstAsync(c => c.Type == TransactionType.Expense);

        var added = await _repository.AddAsync(new Transaction
        {
            Amount = 42.50m,
            Type = TransactionType.Expense,
            CategoryId = category.Id,
            WalletId = _walletId,
            Date = DateTime.Today,
            Note = "Coffee"
        });

        var fetched = await _repository.GetByIdAsync(added.Id);

        Assert.NotNull(fetched);
        Assert.Equal(42.50m, fetched!.Amount);
        Assert.Equal("Coffee", fetched.Note);
    }

    [Fact]
    public async Task DeleteAsync_RemovesTransaction()
    {
        var category = await _db.Categories.FirstAsync(c => c.Type == TransactionType.Expense);
        var added = await _repository.AddAsync(new Transaction
        {
            Amount = 10m,
            Type = TransactionType.Expense,
            CategoryId = category.Id,
            WalletId = _walletId,
            Date = DateTime.Today
        });

        await _repository.DeleteAsync(added.Id);

        Assert.Null(await _repository.GetByIdAsync(added.Id));
    }

    [Fact]
    public async Task GetAllAsync_FiltersByType()
    {
        var expenseCategory = await _db.Categories.FirstAsync(c => c.Type == TransactionType.Expense);
        var incomeCategory = await _db.Categories.FirstAsync(c => c.Type == TransactionType.Income);

        await _repository.AddAsync(new Transaction { Amount = 10m, Type = TransactionType.Expense, CategoryId = expenseCategory.Id, WalletId = _walletId, Date = DateTime.Today });
        await _repository.AddAsync(new Transaction { Amount = 500m, Type = TransactionType.Income, CategoryId = incomeCategory.Id, WalletId = _walletId, Date = DateTime.Today });

        var expenses = await _repository.GetAllAsync(type: TransactionType.Expense);

        Assert.Single(expenses);
        Assert.Equal(TransactionType.Expense, expenses[0].Type);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}
