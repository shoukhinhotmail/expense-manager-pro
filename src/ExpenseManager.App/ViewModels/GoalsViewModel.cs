using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExpenseManager.Core.Entities;
using ExpenseManager.Core.Repositories;

namespace ExpenseManager.App.ViewModels;

public class SavingsGoalRow
{
    public required SavingsGoal Goal { get; init; }

    /// <summary>0-100, clamped — a goal that's overfunded still shows a full bar rather than
    /// overflowing it.</summary>
    public double ProgressPercent => Goal.TargetAmount <= 0
        ? 0
        : Math.Min(100.0, (double)(Goal.CurrentAmount / Goal.TargetAmount * 100m));

    public bool IsComplete => Goal.TargetAmount > 0 && Goal.CurrentAmount >= Goal.TargetAmount;
    public decimal Remaining => Math.Max(0, Goal.TargetAmount - Goal.CurrentAmount);
}

public class BudgetLimitRow
{
    public required BudgetLimit Budget { get; init; }
    public required decimal Spent { get; init; }

    public decimal Remaining => Math.Max(0, Budget.LimitAmount - Spent);
    public double ProgressPercent => Budget.LimitAmount <= 0
        ? 0
        : Math.Min(100.0, (double)(Spent / Budget.LimitAmount * 100m));

    public bool IsOverLimit => Budget.LimitAmount > 0 && Spent > Budget.LimitAmount;
    public string PeriodLabel => Budget.Period == BudgetPeriod.Weekly ? "this week" : "this month";
}

public partial class GoalsViewModel(
    ISavingsGoalRepository goalRepository,
    IBudgetLimitRepository budgetRepository,
    ICategoryRepository categoryRepository) : ViewModelBase
{
    public ObservableCollection<SavingsGoalRow> SavingsGoals { get; } = new();
    public ObservableCollection<BudgetLimitRow> BudgetLimits { get; } = new();
    public ObservableCollection<Category> ExpenseCategories { get; } = new();

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            var categories = await categoryRepository.GetAllAsync(TransactionType.Expense);
            ExpenseCategories.Clear();
            foreach (var category in categories)
                ExpenseCategories.Add(category);

            var goals = await goalRepository.GetAllAsync();
            SavingsGoals.Clear();
            foreach (var goal in goals)
                SavingsGoals.Add(new SavingsGoalRow { Goal = goal });

            var budgets = await budgetRepository.GetAllAsync();
            BudgetLimits.Clear();
            foreach (var budget in budgets)
            {
                var spent = await budgetRepository.GetSpentInCurrentPeriodAsync(budget.Id);
                BudgetLimits.Add(new BudgetLimitRow { Budget = budget, Spent = spent });
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Couldn't load goals: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task<bool> SaveGoalAsync(SavingsGoal goal)
    {
        try
        {
            if (goal.Id == 0)
                await goalRepository.AddAsync(goal);
            else
                await goalRepository.UpdateAsync(goal);
            return true;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Couldn't save goal: {ex.Message}";
            return false;
        }
    }

    [RelayCommand]
    public async Task DeleteGoalAsync(SavingsGoal goal)
    {
        try
        {
            await goalRepository.DeleteAsync(goal.Id);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Couldn't delete goal: {ex.Message}";
        }
    }

    public async Task<bool> AddContributionAsync(int goalId, decimal amount)
    {
        try
        {
            await goalRepository.AddContributionAsync(goalId, amount);
            await LoadAsync();
            return true;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Couldn't update contribution: {ex.Message}";
            return false;
        }
    }

    public async Task<bool> SaveBudgetAsync(BudgetLimit budget)
    {
        try
        {
            if (budget.Id == 0)
                await budgetRepository.AddAsync(budget);
            else
                await budgetRepository.UpdateAsync(budget);
            return true;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Couldn't save budget: {ex.Message}";
            return false;
        }
    }

    [RelayCommand]
    public async Task DeleteBudgetAsync(BudgetLimit budget)
    {
        try
        {
            await budgetRepository.DeleteAsync(budget.Id);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Couldn't delete budget: {ex.Message}";
        }
    }
}
