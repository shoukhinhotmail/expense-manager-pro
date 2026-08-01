using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExpenseManager.Core.Entities;
using ExpenseManager.Core.Repositories;

namespace ExpenseManager.App.ViewModels;

public partial class RecurringTransactionsViewModel(IRecurringTransactionRepository recurringRepository) : ViewModelBase
{
    public ObservableCollection<RecurringTransaction> Items { get; } = new();

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            var items = await recurringRepository.GetAllAsync();
            Items.Clear();
            foreach (var item in items)
                Items.Add(item);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Couldn't load recurring transactions: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task<bool> SaveAsync(RecurringTransaction recurring)
    {
        try
        {
            if (recurring.Id == 0)
                await recurringRepository.AddAsync(recurring);
            else
                await recurringRepository.UpdateAsync(recurring);
            return true;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Couldn't save: {ex.Message}";
            return false;
        }
    }

    [RelayCommand]
    public async Task ToggleActiveAsync(RecurringTransaction recurring)
    {
        recurring.IsActive = !recurring.IsActive;
        await recurringRepository.UpdateAsync(recurring);
    }

    [RelayCommand]
    public async Task DeleteAsync(RecurringTransaction recurring)
    {
        try
        {
            await recurringRepository.DeleteAsync(recurring.Id);
            Items.Remove(recurring);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Couldn't delete: {ex.Message}";
        }
    }
}
