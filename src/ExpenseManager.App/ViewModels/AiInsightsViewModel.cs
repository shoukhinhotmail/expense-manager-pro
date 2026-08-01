using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExpenseManager.App.Services;
using ExpenseManager.Core.Repositories;
using ExpenseManager.Core.Services;

namespace ExpenseManager.App.ViewModels;

public partial class AiInsightsViewModel(
    ISummaryService summaryService,
    IBudgetLimitRepository budgetLimitRepository,
    ISavingsGoalRepository savingsGoalRepository,
    CurrencyService currencyService,
    AiInsightsService aiInsightsService) : ViewModelBase
{
    [ObservableProperty]
    private bool isModelDownloaded;

    [ObservableProperty]
    private bool isDownloading;

    [ObservableProperty]
    private double downloadProgress;

    [ObservableProperty]
    private bool isGenerating;

    [ObservableProperty]
    private string? insightsText;

    private CancellationTokenSource? _downloadCts;

    public void RefreshModelStatus() => IsModelDownloaded = aiInsightsService.IsModelDownloaded;

    [RelayCommand]
    public async Task DownloadModelAsync()
    {
        IsDownloading = true;
        DownloadProgress = 0;
        ErrorMessage = null;
        _downloadCts = new CancellationTokenSource();
        var progress = new Progress<double>(p => DownloadProgress = p);

        try
        {
            await aiInsightsService.DownloadModelAsync(progress, _downloadCts.Token);
            IsModelDownloaded = true;
        }
        catch (OperationCanceledException)
        {
            // User cancelled — not an error.
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Couldn't download the AI model: {ex.Message}";
        }
        finally
        {
            IsDownloading = false;
            _downloadCts = null;
        }
    }

    [RelayCommand]
    public void CancelDownload() => _downloadCts?.Cancel();

    [RelayCommand]
    public void DeleteModel()
    {
        aiInsightsService.DeleteModel();
        IsModelDownloaded = false;
        InsightsText = null;
    }

    [RelayCommand]
    public async Task GenerateInsightsAsync()
    {
        IsGenerating = true;
        ErrorMessage = null;
        InsightsText = null;
        try
        {
            var (system, user) = await BuildPromptAsync();
            var progress = new Progress<string>(text => InsightsText = text);
            InsightsText = await aiInsightsService.GenerateInsightsAsync(system, user, progress);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Couldn't generate insights: {ex.Message}";
        }
        finally
        {
            IsGenerating = false;
        }
    }

    private async Task<(string System, string User)> BuildPromptAsync()
    {
        var today = DateTime.Today;
        var thisMonthStart = new DateTime(today.Year, today.Month, 1);
        var lastMonthStart = thisMonthStart.AddMonths(-1);
        var lastMonthEnd = thisMonthStart.AddDays(-1);

        var thisMonth = await summaryService.GetSummaryAsync(thisMonthStart, today);
        var lastMonth = await summaryService.GetSummaryAsync(lastMonthStart, lastMonthEnd);
        var currency = currencyService.Current.Code;

        var sb = new StringBuilder();
        sb.AppendLine($"Currency: {currency}");
        sb.AppendLine();
        sb.AppendLine("This month so far:");
        sb.AppendLine($"- Income: {thisMonth.TotalIncome:0.##}");
        sb.AppendLine($"- Expenses: {thisMonth.TotalExpense:0.##}");
        sb.AppendLine($"- Net: {thisMonth.TotalIncome - thisMonth.TotalExpense:0.##}");
        sb.AppendLine();
        sb.AppendLine("Last month (full month, for comparison):");
        sb.AppendLine($"- Income: {lastMonth.TotalIncome:0.##}");
        sb.AppendLine($"- Expenses: {lastMonth.TotalExpense:0.##}");
        sb.AppendLine();

        if (thisMonth.ExpenseByCategory.Count > 0)
        {
            sb.AppendLine("Top spending categories this month:");
            foreach (var category in thisMonth.ExpenseByCategory.Take(5))
            {
                var pct = thisMonth.TotalExpense > 0 ? category.Total / thisMonth.TotalExpense * 100m : 0;
                sb.AppendLine($"- {category.CategoryName}: {category.Total:0.##} ({pct:0}% of this month's spending)");
            }
            sb.AppendLine();
        }

        var budgets = await budgetLimitRepository.GetAllAsync();
        if (budgets.Count > 0)
        {
            sb.AppendLine("Budget limits:");
            foreach (var budget in budgets)
            {
                var spent = await budgetLimitRepository.GetSpentInCurrentPeriodAsync(budget.Id);
                var status = spent > budget.LimitAmount ? "OVER LIMIT" : "within limit";
                sb.AppendLine($"- {budget.Category?.Name}: {spent:0.##} spent of {budget.LimitAmount:0.##} {budget.Period} limit ({status})");
            }
            sb.AppendLine();
        }

        var goals = await savingsGoalRepository.GetAllAsync();
        if (goals.Count > 0)
        {
            sb.AppendLine("Savings goals:");
            foreach (var goal in goals)
            {
                var pct = goal.TargetAmount > 0 ? goal.CurrentAmount / goal.TargetAmount * 100m : 0;
                var deadline = goal.TargetDate is { } d ? $", target date {d:MMM d, yyyy}" : "";
                sb.AppendLine($"- {goal.Name}: {goal.CurrentAmount:0.##} of {goal.TargetAmount:0.##} ({pct:0}%){deadline}");
            }
        }

        const string system =
            "You are a friendly, concise personal finance assistant built into a budgeting app. " +
            "The user will give you a summary of their spending, budgets, and savings goals. " +
            "Write 3 to 5 short, specific, actionable insights or recommendations in plain conversational language. " +
            "Use a numbered list. Reference actual numbers and category names from the summary. " +
            "Do not repeat the whole summary back verbatim. Do not give generic advice that ignores the data given. " +
            "Keep each point to 1-2 sentences.";

        return (system, sb.ToString());
    }
}
