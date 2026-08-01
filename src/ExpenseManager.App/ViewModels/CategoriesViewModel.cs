using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExpenseManager.Core.Entities;
using ExpenseManager.Core.Repositories;

namespace ExpenseManager.App.ViewModels;

public partial class CategoriesViewModel(ICategoryRepository categoryRepository) : ViewModelBase
{
    public ObservableCollection<Category> ExpenseCategories { get; } = new();
    public ObservableCollection<Category> IncomeCategories { get; } = new();

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            ExpenseCategories.Clear();
            foreach (var c in await categoryRepository.GetAllAsync(TransactionType.Expense))
                ExpenseCategories.Add(c);

            IncomeCategories.Clear();
            foreach (var c in await categoryRepository.GetAllAsync(TransactionType.Income))
                IncomeCategories.Add(c);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Couldn't load categories: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task<bool> SaveAsync(Category category)
    {
        try
        {
            if (category.Id == 0)
                await categoryRepository.AddAsync(category);
            else
                await categoryRepository.UpdateAsync(category);
            return true;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Couldn't save category: {ex.Message}";
            return false;
        }
    }

    [RelayCommand]
    public async Task DeleteAsync(Category category)
    {
        try
        {
            await categoryRepository.DeleteAsync(category.Id);
            ExpenseCategories.Remove(category);
            IncomeCategories.Remove(category);
        }
        catch (Exception)
        {
            ErrorMessage = "Couldn't delete category. It may still be used by transactions.";
        }
    }
}
