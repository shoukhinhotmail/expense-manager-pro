using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExpenseManager.Core.Entities;
using ExpenseManager.Core.Models;
using ExpenseManager.Core.Repositories;
using ExpenseManager.Core.Services;

namespace ExpenseManager.App.ViewModels;

public enum DashboardPeriod
{
    ThisMonth,
    LastMonth,
    ThisYear,
    AllTime
}

public partial class DashboardViewModel(
    ISummaryService summaryService,
    ITransactionRepository transactionRepository,
    IWalletRepository walletRepository) : ViewModelBase
{
    [ObservableProperty]
    private decimal totalIncome;

    [ObservableProperty]
    private decimal totalExpense;

    [ObservableProperty]
    private decimal balance;

    [ObservableProperty]
    private DashboardPeriod selectedPeriod = DashboardPeriod.ThisMonth;

    public ObservableCollection<CategoryTotal> ExpenseByCategory { get; } = new();
    public ObservableCollection<Transaction> RecentTransactions { get; } = new();
    public ObservableCollection<WalletRow> Wallets { get; } = new();

    partial void OnSelectedPeriodChanged(DashboardPeriod value) => _ = LoadAsync();

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            var (from, to) = GetRange(SelectedPeriod);
            var summary = await summaryService.GetSummaryAsync(from, to);
            TotalIncome = summary.TotalIncome;
            TotalExpense = summary.TotalExpense;
            Balance = summary.Balance;

            ExpenseByCategory.Clear();
            foreach (var item in summary.ExpenseByCategory)
                ExpenseByCategory.Add(item);

            var recent = await transactionRepository.GetAllAsync(from: from, to: to);
            RecentTransactions.Clear();
            foreach (var item in recent.Take(6))
                RecentTransactions.Add(item);

            var wallets = await walletRepository.GetAllAsync();
            Wallets.Clear();
            foreach (var wallet in wallets)
            {
                var walletBalance = await walletRepository.GetCurrentBalanceAsync(wallet.Id);
                Wallets.Add(new WalletRow { Wallet = wallet, Balance = walletBalance });
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Couldn't load dashboard: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static (DateTime From, DateTime To) GetRange(DashboardPeriod period)
    {
        var today = DateTime.Today;
        return period switch
        {
            DashboardPeriod.ThisMonth => (new DateTime(today.Year, today.Month, 1), today),
            DashboardPeriod.LastMonth => (
                new DateTime(today.Year, today.Month, 1).AddMonths(-1),
                new DateTime(today.Year, today.Month, 1).AddDays(-1)),
            DashboardPeriod.ThisYear => (new DateTime(today.Year, 1, 1), today),
            DashboardPeriod.AllTime => (DateTime.MinValue, today),
            _ => (new DateTime(today.Year, today.Month, 1), today)
        };
    }
}
