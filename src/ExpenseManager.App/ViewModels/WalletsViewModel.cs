using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExpenseManager.Core.Entities;
using ExpenseManager.Core.Repositories;

namespace ExpenseManager.App.ViewModels;

public class WalletRow
{
    public required Wallet Wallet { get; init; }
    public decimal Balance { get; init; }
}

public partial class WalletsViewModel(IWalletRepository walletRepository) : ViewModelBase
{
    public ObservableCollection<WalletRow> Wallets { get; } = new();

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            var wallets = await walletRepository.GetAllAsync();
            Wallets.Clear();
            foreach (var wallet in wallets)
            {
                var balance = await walletRepository.GetCurrentBalanceAsync(wallet.Id);
                Wallets.Add(new WalletRow { Wallet = wallet, Balance = balance });
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Couldn't load wallets: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task<bool> SaveAsync(Wallet wallet)
    {
        try
        {
            if (wallet.Id == 0)
                await walletRepository.AddAsync(wallet);
            else
                await walletRepository.UpdateAsync(wallet);
            return true;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Couldn't save wallet: {ex.Message}";
            return false;
        }
    }

    [RelayCommand]
    public async Task DeleteAsync(Wallet wallet)
    {
        try
        {
            await walletRepository.DeleteAsync(wallet.Id);
            await LoadAsync();
        }
        catch (Exception)
        {
            ErrorMessage = "Couldn't delete wallet. It may still be used by transactions — try archiving it instead.";
        }
    }
}
