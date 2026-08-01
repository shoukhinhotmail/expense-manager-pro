using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExpenseManager.Core.Entities;
using ExpenseManager.Core.Repositories;

namespace ExpenseManager.App.ViewModels;

public abstract partial class TransactionListViewModel(
    ITransactionRepository transactionRepository,
    ICategoryRepository categoryRepository,
    IWalletRepository walletRepository,
    TransactionType type) : ViewModelBase
{
    public static readonly Category AllCategoriesOption = new() { Id = 0, Name = "All categories" };
    public static readonly Wallet AllWalletsOption = new() { Id = 0, Name = "All wallets" };

    [ObservableProperty]
    private string? searchText;

    [ObservableProperty]
    private decimal total;

    [ObservableProperty]
    private Category? filterCategory;

    [ObservableProperty]
    private Wallet? filterWallet;

    [ObservableProperty]
    private DateTimeOffset? filterDateFrom;

    [ObservableProperty]
    private DateTimeOffset? filterDateTo;

    [ObservableProperty]
    private double filterMinAmount = double.NaN;

    [ObservableProperty]
    private double filterMaxAmount = double.NaN;

    public ObservableCollection<Transaction> Transactions { get; } = new();

    public ObservableCollection<Category> Categories { get; } = new();

    public ObservableCollection<Wallet> Wallets { get; } = new();

    public TransactionType Type => type;

    /// <summary>Populates the filter dropdowns. Call once when the page is navigated to, before
    /// LoadAsync — separate from it since filter options rarely change but transactions do.</summary>
    public async Task LoadFiltersAsync()
    {
        var categories = await categoryRepository.GetAllAsync(type);
        Categories.Clear();
        Categories.Add(AllCategoriesOption);
        foreach (var category in categories)
            Categories.Add(category);
        FilterCategory ??= AllCategoriesOption;

        var wallets = await walletRepository.GetAllAsync();
        Wallets.Clear();
        Wallets.Add(AllWalletsOption);
        foreach (var wallet in wallets)
            Wallets.Add(wallet);
        FilterWallet ??= AllWalletsOption;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            var items = await transactionRepository.GetAllAsync(
                type: type,
                from: FilterDateFrom?.Date,
                to: FilterDateTo?.Date,
                categoryId: FilterCategory is { Id: > 0 } ? FilterCategory.Id : null,
                walletId: FilterWallet is { Id: > 0 } ? FilterWallet.Id : null,
                searchText: SearchText,
                minAmount: double.IsNaN(FilterMinAmount) ? null : (decimal)FilterMinAmount,
                maxAmount: double.IsNaN(FilterMaxAmount) ? null : (decimal)FilterMaxAmount);
            Transactions.Clear();
            foreach (var item in items)
                Transactions.Add(item);
            Total = items.Sum(t => t.Amount);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Couldn't load transactions: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task ClearFiltersAsync()
    {
        FilterCategory = AllCategoriesOption;
        FilterWallet = AllWalletsOption;
        FilterDateFrom = null;
        FilterDateTo = null;
        FilterMinAmount = double.NaN;
        FilterMaxAmount = double.NaN;
        SearchText = null;
        await LoadAsync();
    }

    public async Task<bool> SaveAsync(Transaction transaction)
    {
        try
        {
            if (transaction.Id == 0)
                await transactionRepository.AddAsync(transaction);
            else
                await transactionRepository.UpdateAsync(transaction);
            return true;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Couldn't save: {ex.Message}";
            return false;
        }
    }

    [RelayCommand]
    public async Task DeleteAsync(Transaction transaction)
    {
        try
        {
            await transactionRepository.DeleteAsync(transaction.Id);
            Transactions.Remove(transaction);
            Total = Transactions.Sum(t => t.Amount);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Couldn't delete: {ex.Message}";
        }
    }
}
