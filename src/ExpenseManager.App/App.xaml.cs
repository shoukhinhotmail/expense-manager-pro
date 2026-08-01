using ExpenseManager.App.Services;
using ExpenseManager.App.ViewModels;
using ExpenseManager.Core.Repositories;
using ExpenseManager.Core.Services;
using ExpenseManager.Data;
using ExpenseManager.Data.Repositories;
using ExpenseManager.Data.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;

namespace ExpenseManager.App;

public partial class App : Application
{
    public static IHost Host { get; private set; } = null!;
    public static Window? MainAppWindow { get; private set; }

    public App()
    {
        InitializeComponent();

        var dataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ExpenseManagerPro");
        Directory.CreateDirectory(dataDir);
        var dbPath = Path.Combine(dataDir, "expensemanager.db");

        Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddDbContext<ExpenseManagerDbContext>(options =>
                    options.UseSqlite($"Data Source={dbPath}"));

                services.AddScoped<ICategoryRepository, CategoryRepository>();
                services.AddScoped<ITransactionRepository, TransactionRepository>();
                services.AddScoped<IWalletRepository, WalletRepository>();
                services.AddScoped<IRecurringTransactionRepository, RecurringTransactionRepository>();
                services.AddScoped<ISavingsGoalRepository, SavingsGoalRepository>();
                services.AddScoped<IBudgetLimitRepository, BudgetLimitRepository>();
                services.AddScoped<ISummaryService, SummaryService>();
                services.AddScoped<RecurringService>();

                services.AddSingleton<ThemeService>();
                services.AddSingleton<SettingsService>();
                services.AddSingleton<CurrencyService>();
                services.AddSingleton<BackupService>();
                services.AddSingleton<LockService>();
                services.AddSingleton<NotificationService>();
                services.AddSingleton<DashboardLayoutService>();
                services.AddSingleton<ExportService>();
                services.AddSingleton<GoogleDriveBackupService>();
                services.AddSingleton<AiInsightsService>();
                services.AddSingleton<ShareService>();
                services.AddSingleton<ShareCardService>();

                services.AddTransient<DashboardViewModel>();
                services.AddTransient<ExpensesViewModel>();
                services.AddTransient<IncomeViewModel>();
                services.AddTransient<CategoriesViewModel>();
                services.AddTransient<WalletsViewModel>();
                services.AddTransient<RecurringTransactionsViewModel>();
                services.AddTransient<TransactionEditViewModel>();
                services.AddTransient<GoalsViewModel>();
                services.AddTransient<AiInsightsViewModel>();

                services.AddSingleton<MainWindow>();
            })
            .Build();
    }

    private SplashWindow? _splash;

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        Host.Services.GetRequiredService<NotificationService>().Register();

        _splash = new SplashWindow();
        _splash.Activate();

        await Task.Run(() =>
        {
            using var scope = Host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ExpenseManagerDbContext>();
            db.Database.Migrate();
        });

        // A branded splash that flashes for a single frame reads as broken, not fast — give it a
        // minimum visible time so it feels like an intentional launch moment.
        await Task.Delay(500);

        var currencyService = Host.Services.GetRequiredService<CurrencyService>();
        var lockService = Host.Services.GetRequiredService<LockService>();

        if (!currencyService.IsConfigured)
        {
            var onboarding = new OnboardingWindow();
            onboarding.Activate();
        }
        else if (lockService.IsPinLockEnabled)
        {
            var lockWindow = new LockWindow(lockService);
            lockWindow.Activate();
        }
        else
        {
            LaunchMainWindow();
        }

        _splash.Close();
        _splash = null;
    }

    public static void LaunchMainWindow()
    {
        MainAppWindow = Host.Services.GetRequiredService<MainWindow>();
        MainAppWindow.Activate();
        _ = ProcessRecurringAndNotifyAsync();
        _ = TryAutoBackupToGoogleDriveAsync();
    }

    private static async Task TryAutoBackupToGoogleDriveAsync()
    {
        var settings = Host.Services.GetRequiredService<SettingsService>();
        if (!settings.Current.IsGoogleDriveAutoBackupEnabled) return;

        var lastBackup = settings.Current.LastGoogleDriveBackupUtc;
        if (lastBackup is not null && DateTime.UtcNow - lastBackup.Value < TimeSpan.FromDays(1)) return;

        var googleDrive = Host.Services.GetRequiredService<GoogleDriveBackupService>();
        if (!googleDrive.IsSignedIn && !await googleDrive.TryRestoreSessionAsync()) return;

        try
        {
            var backupService = Host.Services.GetRequiredService<BackupService>();
            var tempPath = Path.Combine(Path.GetTempPath(), $"ExpenseManagerPro-AutoBackup-{Guid.NewGuid():N}.embackup");
            await backupService.BackupToAsync(tempPath);
            try
            {
                await googleDrive.UploadBackupAsync(tempPath);
            }
            finally
            {
                File.Delete(tempPath);
            }

            settings.Current.LastGoogleDriveBackupUtc = DateTime.UtcNow;
            settings.Save();

            Host.Services.GetRequiredService<NotificationService>()
                .Show("Backed up to Google Drive", "Your data was automatically backed up.");
        }
        catch
        {
            // Auto backup is best-effort — a failure here (offline, revoked token, etc.) shouldn't
            // interrupt the user; they can always back up manually from Settings.
        }
    }

    private static async Task ProcessRecurringAndNotifyAsync()
    {
        var notifications = Host.Services.GetRequiredService<NotificationService>();
        using var scope = Host.Services.CreateScope();
        var recurringService = scope.ServiceProvider.GetRequiredService<RecurringService>();

        var generated = await recurringService.ProcessDueAsync();
        if (generated.Count > 0)
        {
            notifications.Show(
                "Recurring transactions added",
                generated.Count == 1 ? generated[0] : $"{generated.Count} schedules posted: {string.Join(", ", generated)}");
        }

        var upcoming = await recurringService.GetUpcomingAsync();
        foreach (var item in upcoming.Where(r => (r.NextDueDate.Date - DateTime.Today).Days <= r.ReminderDaysBefore))
        {
            var categoryName = item.Category?.Name ?? "Uncategorized";
            var dueText = item.NextDueDate.Date == DateTime.Today ? "today" : $"on {item.NextDueDate:MMM d}";
            notifications.Show("Upcoming recurring transaction", $"{categoryName} — {item.Amount:0.##} due {dueText}");
        }
    }
}
