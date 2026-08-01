using ExpenseManager.App.Services;
using ExpenseManager.Core.Entities;
using ExpenseManager.Core.Models;
using ExpenseManager.Core.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace ExpenseManager.App.Views;

public sealed partial class ExportPage : Page
{
    private static readonly Wallet AllWalletsOption = new() { Id = 0, Name = "All wallets" };

    private readonly ITransactionRepository _transactionRepository;
    private readonly IWalletRepository _walletRepository;
    private readonly ExportService _exportService;
    private readonly ShareService _shareService;
    private readonly ShareCardService _shareCardService;

    public ExportPage()
    {
        _transactionRepository = App.Host.Services.GetRequiredService<ITransactionRepository>();
        _walletRepository = App.Host.Services.GetRequiredService<IWalletRepository>();
        _exportService = App.Host.Services.GetRequiredService<ExportService>();
        _shareService = App.Host.Services.GetRequiredService<ShareService>();
        _shareCardService = App.Host.Services.GetRequiredService<ShareCardService>();
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        var wallets = await _walletRepository.GetAllAsync();
        var items = new List<Wallet> { AllWalletsOption };
        items.AddRange(wallets);
        WalletCombo.ItemsSource = items;
        WalletCombo.SelectedIndex = 0;

        await UpdateMatchCountAsync();
    }

    private async void Filter_Changed(object sender, SelectionChangedEventArgs e) => await UpdateMatchCountAsync();

    private (DateTime From, DateTime To) GetRange()
    {
        var today = DateTime.Today;
        return PeriodCombo.SelectedIndex switch
        {
            1 => (new DateTime(today.Year, today.Month, 1).AddMonths(-1), new DateTime(today.Year, today.Month, 1).AddDays(-1)),
            2 => (new DateTime(today.Year, 1, 1), today),
            3 => (DateTime.MinValue, today),
            _ => (new DateTime(today.Year, today.Month, 1), today)
        };
    }

    private TransactionType? GetTypeFilter() => TypeCombo.SelectedIndex switch
    {
        1 => TransactionType.Expense,
        2 => TransactionType.Income,
        _ => null
    };

    private int? GetWalletFilter() =>
        WalletCombo.SelectedItem is Wallet { Id: > 0 } wallet ? wallet.Id : null;

    private async Task<List<Transaction>> GetFilteredTransactionsAsync()
    {
        var (from, to) = GetRange();
        return await _transactionRepository.GetAllAsync(type: GetTypeFilter(), from: from, to: to, walletId: GetWalletFilter());
    }

    private async Task UpdateMatchCountAsync()
    {
        if (!IsLoaded) return;
        var transactions = await GetFilteredTransactionsAsync();
        MatchCountText.Text = transactions.Count == 1
            ? "1 transaction matches this filter"
            : $"{transactions.Count} transactions match this filter";
    }

    private async void ExportCsv_Click(object sender, RoutedEventArgs e) =>
        await ExportAsync("CSV file", [".csv"], async (transactions, path) => await _exportService.ExportCsvAsync(transactions, path));

    private async void ExportJson_Click(object sender, RoutedEventArgs e) =>
        await ExportAsync("JSON file", [".json"], async (transactions, path) => await _exportService.ExportJsonAsync(transactions, path));

    private async void ExportExcel_Click(object sender, RoutedEventArgs e) =>
        await ExportAsync("Excel workbook", [".xlsx"], async (transactions, path) => await _exportService.ExportExcelAsync(transactions, path));

    private async void ExportPdf_Click(object sender, RoutedEventArgs e)
    {
        await ExportAsync("PDF report", [".pdf"], async (transactions, path) =>
        {
            var (from, to) = GetRange();
            // Computed from the same filtered list as the transaction table below it, so the
            // summary always matches the period/type/wallet filters currently selected — not
            // silently recalculated across all wallets/types from the database.
            var summary = ComputeSummary(transactions);
            await _exportService.ExportPdfReportAsync(summary, transactions, from, to, path);
        });
    }

    private static DashboardSummary ComputeSummary(List<Transaction> transactions)
    {
        var summary = new DashboardSummary
        {
            TotalIncome = transactions.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount),
            TotalExpense = transactions.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount)
        };

        summary.ExpenseByCategory = transactions
            .Where(t => t.Type == TransactionType.Expense && t.Category is not null)
            .GroupBy(t => t.CategoryId)
            .Select(g => new CategoryTotal
            {
                CategoryId = g.Key,
                CategoryName = g.First().Category!.Name,
                Color = g.First().Category!.Color,
                Total = g.Sum(t => t.Amount)
            })
            .OrderByDescending(c => c.Total)
            .ToList();

        return summary;
    }

    private async Task ExportAsync(string choiceName, string[] extensions, Func<List<Transaction>, string, Task> exportAction)
    {
        var picker = new FileSavePicker();
        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(App.MainAppWindow));
        picker.SuggestedStartLocation = PickerLocationId.Downloads;
        picker.FileTypeChoices.Add(choiceName, extensions);
        picker.SuggestedFileName = $"ExpenseManagerPro-Export-{DateTime.Now:yyyy-MM-dd}";

        var file = await picker.PickSaveFileAsync();
        if (file is null) return;

        try
        {
            var transactions = await GetFilteredTransactionsAsync();
            await exportAction(transactions, file.Path);
            await ShowMessageAsync("Export complete", $"Saved to:\n{file.Path}");
        }
        catch (Exception ex)
        {
            await ShowMessageAsync("Export failed", ex.Message);
        }
    }

    private async void ShareSquareCard_Click(object sender, RoutedEventArgs e) => await ShareCardAsync(ShareCardSize.Square);
    private async void ShareStoryCard_Click(object sender, RoutedEventArgs e) => await ShareCardAsync(ShareCardSize.Story);
    private async void ShareLandscapeCard_Click(object sender, RoutedEventArgs e) => await ShareCardAsync(ShareCardSize.Landscape);

    private async Task ShareCardAsync(ShareCardSize size)
    {
        try
        {
            var cardPath = await GenerateCardAsync(size);
            var hwnd = WindowNative.GetWindowHandle(App.MainAppWindow);
            await _shareService.ShareFileAsync(hwnd, cardPath, "My spending summary", "Shared from Expense Manager Pro");
        }
        catch (Exception ex)
        {
            await ShowMessageAsync("Share failed", ex.Message);
        }
    }

    private async void CopyCardToClipboard_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var cardPath = await GenerateCardAsync(ShareCardSize.Square);
            await ShareService.CopyFileToClipboardAsync(cardPath);
            await ShowMessageAsync("Copied", "The summary card image was copied to your clipboard.");
        }
        catch (Exception ex)
        {
            await ShowMessageAsync("Copy failed", ex.Message);
        }
    }

    private async Task<string> GenerateCardAsync(ShareCardSize size)
    {
        var transactions = await GetFilteredTransactionsAsync();
        var (from, to) = GetRange();
        var summary = ComputeSummary(transactions);
        var path = Path.Combine(Path.GetTempPath(), $"ExpenseManagerPro-Card-{Guid.NewGuid():N}.png");
        return _shareCardService.GenerateCard(summary, from, to, size, path);
    }

    private async void ShareCsv_Click(object sender, RoutedEventArgs e) =>
        await ShareExportAsync(".csv", async (transactions, path) => await _exportService.ExportCsvAsync(transactions, path));

    private async void ShareExcel_Click(object sender, RoutedEventArgs e) =>
        await ShareExportAsync(".xlsx", async (transactions, path) => await _exportService.ExportExcelAsync(transactions, path));

    private async void ShareJson_Click(object sender, RoutedEventArgs e) =>
        await ShareExportAsync(".json", async (transactions, path) => await _exportService.ExportJsonAsync(transactions, path));

    private async void SharePdf_Click(object sender, RoutedEventArgs e) =>
        await ShareExportAsync(".pdf", async (transactions, path) =>
        {
            var (from, to) = GetRange();
            var summary = ComputeSummary(transactions);
            await _exportService.ExportPdfReportAsync(summary, transactions, from, to, path);
        });

    private async Task ShareExportAsync(string extension, Func<List<Transaction>, string, Task> exportAction)
    {
        try
        {
            var transactions = await GetFilteredTransactionsAsync();
            var tempPath = Path.Combine(Path.GetTempPath(), $"ExpenseManagerPro-Export-{Guid.NewGuid():N}{extension}");
            await exportAction(transactions, tempPath);

            var hwnd = WindowNative.GetWindowHandle(App.MainAppWindow);
            await _shareService.ShareFileAsync(hwnd, tempPath, "My expense report", "Shared from Expense Manager Pro");
        }
        catch (Exception ex)
        {
            await ShowMessageAsync("Share failed", ex.Message);
        }
    }

    private async Task ShowMessageAsync(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = XamlRoot
        };
        await dialog.ShowAsync();
    }
}
