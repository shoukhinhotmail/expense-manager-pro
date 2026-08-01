using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using ExpenseManager.Core.Entities;
using ExpenseManager.Core.Repositories;

namespace ExpenseManager.App.ViewModels;

public partial class TransactionEditViewModel(ICategoryRepository categoryRepository, IWalletRepository walletRepository) : ViewModelBase
{
    private int? _editingId;

    [ObservableProperty]
    private TransactionType type = TransactionType.Expense;

    [ObservableProperty]
    private string amountText = string.Empty;

    [ObservableProperty]
    private DateTimeOffset date = DateTimeOffset.Now;

    [ObservableProperty]
    private string? note;

    [ObservableProperty]
    private Category? selectedCategory;

    [ObservableProperty]
    private Wallet? selectedWallet;

    public ObservableCollection<Category> Categories { get; } = new();
    public ObservableCollection<Wallet> Wallets { get; } = new();

    public bool IsEditing => _editingId is not null;

    public async Task InitializeAsync(TransactionType transactionType, Transaction? existing)
    {
        Type = transactionType;
        ErrorMessage = null;

        Categories.Clear();
        var categories = await categoryRepository.GetAllAsync(transactionType);
        foreach (var category in categories)
            Categories.Add(category);

        Wallets.Clear();
        var wallets = await walletRepository.GetAllAsync();
        foreach (var wallet in wallets)
            Wallets.Add(wallet);

        if (existing is not null)
        {
            _editingId = existing.Id;
            AmountText = existing.Amount.ToString("0.##");
            Date = new DateTimeOffset(existing.Date);
            Note = existing.Note;
            SelectedCategory = Categories.FirstOrDefault(c => c.Id == existing.CategoryId);
            SelectedWallet = Wallets.FirstOrDefault(w => w.Id == existing.WalletId);
        }
        else
        {
            _editingId = null;
            AmountText = string.Empty;
            Date = DateTimeOffset.Now;
            Note = null;
            SelectedCategory = Categories.FirstOrDefault();
            SelectedWallet = Wallets.FirstOrDefault(w => w.IsDefault) ?? Wallets.FirstOrDefault();
        }
    }

    /// <summary>Creates a new category (scoped to the current Type) and selects it — used by the
    /// "add new category" shortcut inside the transaction form.</summary>
    public async Task<Category> AddCategoryAsync(string name, string color)
    {
        var category = new Category { Name = name, Color = color, Type = Type, Glyph = "" };
        await categoryRepository.AddAsync(category);
        Categories.Add(category);
        SelectedCategory = category;
        return category;
    }

    /// <summary>Validates the form and builds a Transaction. Returns null (with ErrorMessage set) if invalid.</summary>
    public Transaction? TryBuildTransaction()
    {
        if (!decimal.TryParse(AmountText, out var amount) || amount <= 0)
        {
            ErrorMessage = "Enter a valid amount greater than zero.";
            return null;
        }

        if (SelectedCategory is null)
        {
            ErrorMessage = "Choose a category.";
            return null;
        }

        if (SelectedWallet is null)
        {
            ErrorMessage = "Choose a wallet.";
            return null;
        }

        ErrorMessage = null;
        return new Transaction
        {
            Id = _editingId ?? 0,
            Amount = amount,
            Type = Type,
            Date = Date.Date,
            Note = string.IsNullOrWhiteSpace(Note) ? null : Note.Trim(),
            CategoryId = SelectedCategory.Id,
            WalletId = SelectedWallet.Id
        };
    }
}
