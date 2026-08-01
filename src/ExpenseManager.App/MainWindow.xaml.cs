using ExpenseManager.App.Services;
using ExpenseManager.App.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Xaml.Controls;
using Windows.UI;

namespace ExpenseManager.App;

public sealed partial class MainWindow : Window
{
    private readonly ThemeService _themeService;

    public MainWindow(ThemeService themeService)
    {
        _themeService = themeService;
        InitializeComponent();
        Title = "Expense Manager Pro";

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        AppWindow.SetIcon("Assets/app.ico");

        AppWindow.TitleBar.ButtonBackgroundColor = Color.FromArgb(0, 0, 0, 0);
        AppWindow.TitleBar.ButtonInactiveBackgroundColor = Color.FromArgb(0, 0, 0, 0);

        _themeService.ThemeChanged += (_, _) => UpdateTitleBarButtonColors();
    }

    private void NavView_Loaded(object sender, RoutedEventArgs e)
    {
        if (Content is FrameworkElement root)
            _themeService.ApplyTheme(root, _themeService.CurrentTheme);

        UpdateTitleBarButtonColors();

        NavView.SelectedItem = NavView.MenuItems[0];
        ContentFrame.Navigate(typeof(DashboardPage));
    }

    private void UpdateTitleBarButtonColors()
    {
        if (Content is not FrameworkElement root) return;

        var isDark = ThemeService.ResolveActualTheme(root) == ElementTheme.Dark;
        var foreground = isDark ? Colors.White : Colors.Black;

        AppWindow.TitleBar.ButtonForegroundColor = foreground;
        AppWindow.TitleBar.ButtonHoverForegroundColor = foreground;
        AppWindow.TitleBar.ButtonPressedForegroundColor = foreground;
        AppWindow.TitleBar.ButtonInactiveForegroundColor = foreground;
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
        {
            ContentFrame.Navigate(typeof(SettingsPage));
            return;
        }

        if (args.SelectedItemContainer is NavigationViewItem { Tag: string tag })
        {
            Type pageType = tag switch
            {
                "Dashboard" => typeof(DashboardPage),
                "Wallets" => typeof(WalletsPage),
                "Expenses" => typeof(ExpensesPage),
                "Income" => typeof(IncomePage),
                "Categories" => typeof(CategoriesPage),
                "Recurring" => typeof(RecurringTransactionsPage),
                "Goals" => typeof(GoalsPage),
                "AiInsights" => typeof(AiInsightsPage),
                "Export" => typeof(ExportPage),
                _ => typeof(DashboardPage)
            };
            ContentFrame.Navigate(pageType);
        }
    }
}
