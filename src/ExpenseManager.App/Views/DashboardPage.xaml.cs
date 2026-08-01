using ExpenseManager.App.Controls;
using ExpenseManager.App.Services;
using ExpenseManager.App.ViewModels;
using ExpenseManager.Core.Entities;
using ExpenseManager.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using Windows.UI;

namespace ExpenseManager.App.Views;

public sealed partial class DashboardPage : Page
{
    public DashboardViewModel ViewModel { get; }

    private readonly DashboardLayoutService _layoutService;
    private readonly CurrencyService _currencyService;

    public DashboardPage()
    {
        ViewModel = App.Host.Services.GetRequiredService<DashboardViewModel>();
        _layoutService = App.Host.Services.GetRequiredService<DashboardLayoutService>();
        _currencyService = App.Host.Services.GetRequiredService<CurrencyService>();
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await ViewModel.LoadAsync();
        RenderWidgets();
    }

    private async void PeriodCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ViewModel.SelectedPeriod = PeriodCombo.SelectedIndex switch
        {
            1 => DashboardPeriod.LastMonth,
            2 => DashboardPeriod.ThisYear,
            3 => DashboardPeriod.AllTime,
            _ => DashboardPeriod.ThisMonth
        };
        // SelectedPeriod's own change handler triggers LoadAsync; wait a tick for it, then redraw.
        await Task.Delay(50);
        RenderWidgets();
    }

    private void RenderWidgets()
    {
        WidgetsHost.Children.Clear();
        foreach (var config in _layoutService.GetLayout())
        {
            if (!config.IsVisible) continue;
            var widget = config.Widget switch
            {
                DashboardWidget.Summary => BuildSummaryWidget(),
                DashboardWidget.Wallets => BuildWalletsWidget(),
                DashboardWidget.Charts => BuildChartsWidget(),
                DashboardWidget.RecentTransactions => BuildRecentTransactionsWidget(),
                _ => null
            };
            if (widget is not null)
                WidgetsHost.Children.Add(widget);
        }
    }

    private async void CustomizeButton_Click(object sender, RoutedEventArgs e)
    {
        var layout = _layoutService.GetLayout();
        var listView = new ListView { SelectionMode = ListViewSelectionMode.None, CanReorderItems = true, AllowDrop = true };

        foreach (var config in layout)
            listView.Items.Add(BuildCustomizeRow(config));

        var dialog = new ContentDialog
        {
            Title = "Customize dashboard",
            Content = new StackPanel
            {
                Spacing = 8,
                MinWidth = 360,
                Children =
                {
                    new TextBlock
                    {
                        Text = "Drag to reorder. Uncheck to hide a section.",
                        Opacity = 0.7,
                        FontSize = 12,
                        Margin = new Thickness(0, 0, 0, 4)
                    },
                    listView
                }
            },
            PrimaryButtonText = "Save",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary) return;

        var newLayout = new List<DashboardWidgetConfig>();
        foreach (var item in listView.Items)
        {
            if (item is FrameworkElement { Tag: DashboardWidgetConfig config })
                newLayout.Add(config);
        }

        _layoutService.SaveLayout(newLayout);
        RenderWidgets();
    }

    private static FrameworkElement BuildCustomizeRow(DashboardWidgetConfig config)
    {
        var checkbox = new CheckBox
        {
            Content = DashboardLayoutService.DisplayName(config.Widget),
            IsChecked = config.IsVisible
        };
        checkbox.Checked += (_, _) => config.IsVisible = true;
        checkbox.Unchecked += (_, _) => config.IsVisible = false;

        var row = new Grid { Tag = config, Padding = new Thickness(4, 8, 4, 8), ColumnSpacing = 8 };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        Grid.SetColumn(checkbox, 0);
        row.Children.Add(checkbox);
        return row;
    }

    private FrameworkElement BuildSummaryWidget()
    {
        var grid = new Grid { ColumnSpacing = 16 };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var income = BuildStatCard("Income", _currencyService.Format(ViewModel.TotalIncome),
            "", (Brush)Application.Current.Resources["AppIncomeBrush"], Color.FromArgb(0x1A, 0x16, 0xA3, 0x4A));
        var expense = BuildStatCard("Expenses", _currencyService.Format(ViewModel.TotalExpense),
            "", (Brush)Application.Current.Resources["AppExpenseBrush"], Color.FromArgb(0x1A, 0xDC, 0x26, 0x26));
        var balance = BuildStatCard("Balance", _currencyService.Format(ViewModel.Balance),
            "", (Brush)Application.Current.Resources["AppAccentBrush"], null);

        Grid.SetColumn(income, 0);
        Grid.SetColumn(expense, 1);
        Grid.SetColumn(balance, 2);
        grid.Children.Add(income);
        grid.Children.Add(expense);
        grid.Children.Add(balance);
        return grid;
    }

    private FrameworkElement BuildStatCard(string label, string valueText, string glyph, Brush accentBrush, Color? chipBackground)
    {
        var chip = new Border
        {
            Width = 36,
            Height = 36,
            CornerRadius = (CornerRadius)Application.Current.Resources["AppChipCornerRadius"],
            Background = chipBackground is not null
                ? new SolidColorBrush(chipBackground.Value)
                : (Brush)Application.Current.Resources["AppAccentSubtleBrush"],
            Child = new FontIcon
            {
                Glyph = glyph,
                FontSize = 15,
                Foreground = accentBrush,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };

        var header = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
        header.Children.Add(chip);
        header.Children.Add(new TextBlock
        {
            Text = label,
            VerticalAlignment = VerticalAlignment.Center,
            Style = (Style)Application.Current.Resources["AppMutedTextStyle"]
        });

        var valueBlock = new TextBlock
        {
            Text = valueText,
            FontSize = 26,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        };
        if (label != "Balance")
            valueBlock.Foreground = accentBrush;

        var content = new StackPanel { Spacing = 10 };
        content.Children.Add(header);
        content.Children.Add(valueBlock);

        return new Border { Style = (Style)Application.Current.Resources["AppCardStyle"], Child = content };
    }

    private FrameworkElement BuildWalletsWidget()
    {
        var panel = new StackPanel { Spacing = 12 };
        panel.Children.Add(new TextBlock { Text = "Wallets", Style = (Style)Application.Current.Resources["AppSectionHeaderStyle"] });

        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
        foreach (var walletRow in ViewModel.Wallets)
        {
            var iconChip = new Border
            {
                Width = 28,
                Height = 28,
                CornerRadius = (CornerRadius)Application.Current.Resources["AppChipCornerRadius"],
                Background = new SolidColorBrush(HexToColor(walletRow.Wallet.Color, 38)),
                Child = new FontIcon
                {
                    Glyph = WalletGlyph(walletRow.Wallet.Type),
                    FontSize = 13,
                    Foreground = new SolidColorBrush(HexToColor(walletRow.Wallet.Color, 255)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };

            var header = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            header.Children.Add(iconChip);
            header.Children.Add(new TextBlock
            {
                Text = walletRow.Wallet.Name,
                FontSize = 13,
                Style = (Style)Application.Current.Resources["AppMutedTextStyle"],
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            });

            var content = new StackPanel { Spacing = 8 };
            content.Children.Add(header);
            content.Children.Add(new TextBlock
            {
                Text = _currencyService.Format(walletRow.Balance),
                FontSize = 18,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
            });

            row.Children.Add(new Border
            {
                Style = (Style)Application.Current.Resources["AppCardStyle"],
                Width = 200,
                Padding = new Thickness(16),
                Child = content
            });
        }

        panel.Children.Add(new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = row
        });
        return panel;
    }

    private FrameworkElement BuildChartsWidget()
    {
        var grid = new Grid { ColumnSpacing = 16 };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var barChart = new BarChartControl
        {
            Height = 200,
            IncomeValue = ViewModel.TotalIncome,
            ExpenseValue = ViewModel.TotalExpense
        };
        var barContent = new StackPanel { Spacing = 12 };
        barContent.Children.Add(new TextBlock { Text = "Income vs. Expenses", Style = (Style)Application.Current.Resources["AppSectionHeaderStyle"] });
        barContent.Children.Add(barChart);
        var barCard = new Border { Style = (Style)Application.Current.Resources["AppCardStyle"], Child = barContent };

        var pieChart = new PieChartControl { Width = 140, Height = 140, ItemsSource = ViewModel.ExpenseByCategory };
        var legend = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        foreach (var item in ViewModel.ExpenseByCategory)
        {
            var legendRow = new Grid { Padding = new Thickness(0, 4, 0, 4), ColumnSpacing = 8 };
            legendRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            legendRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            legendRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var dot = new Ellipse { Width = 8, Height = 8, VerticalAlignment = VerticalAlignment.Center, Fill = new SolidColorBrush(HexToColor(item.Color, 255)) };
            var name = new TextBlock { Text = item.CategoryName, TextTrimming = TextTrimming.CharacterEllipsis, FontSize = 13 };
            var total = new TextBlock { Text = _currencyService.Format(item.Total), FontSize = 13, Opacity = 0.75 };
            Grid.SetColumn(dot, 0);
            Grid.SetColumn(name, 1);
            Grid.SetColumn(total, 2);
            legendRow.Children.Add(dot);
            legendRow.Children.Add(name);
            legendRow.Children.Add(total);
            legend.Children.Add(legendRow);
        }

        var chartRow = new Grid { ColumnSpacing = 16 };
        chartRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        chartRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        Grid.SetColumn(pieChart, 0);
        Grid.SetColumn(legend, 1);
        chartRow.Children.Add(pieChart);
        chartRow.Children.Add(legend);

        var pieContent = new StackPanel { Spacing = 12 };
        pieContent.Children.Add(new TextBlock { Text = "Spending by category", Style = (Style)Application.Current.Resources["AppSectionHeaderStyle"] });
        pieContent.Children.Add(chartRow);
        var pieCard = new Border { Style = (Style)Application.Current.Resources["AppCardStyle"], Child = pieContent };

        Grid.SetColumn(barCard, 0);
        Grid.SetColumn(pieCard, 1);
        grid.Children.Add(barCard);
        grid.Children.Add(pieCard);
        return grid;
    }

    private FrameworkElement BuildRecentTransactionsWidget()
    {
        var content = new StackPanel { Spacing = 12 };
        content.Children.Add(new TextBlock { Text = "Recent transactions", Style = (Style)Application.Current.Resources["AppSectionHeaderStyle"] });

        var avatarTemplate = (DataTemplate)Application.Current.Resources["CategoryAvatarTemplate"];

        if (ViewModel.RecentTransactions.Count == 0)
        {
            var empty = new StackPanel { Spacing = 8, Padding = new Thickness(0, 24, 0, 24), HorizontalAlignment = HorizontalAlignment.Center };
            empty.Children.Add(new FontIcon { Glyph = "", FontSize = 28, Opacity = 0.4, HorizontalAlignment = HorizontalAlignment.Center });
            empty.Children.Add(new TextBlock { Text = "No transactions yet for this period", Style = (Style)Application.Current.Resources["AppMutedTextStyle"] });
            content.Children.Add(empty);
        }
        else
        {
            foreach (var transaction in ViewModel.RecentTransactions)
                content.Children.Add(BuildTransactionRow(transaction, avatarTemplate));
        }

        return new Border { Style = (Style)Application.Current.Resources["AppCardStyle"], Child = content };
    }

    private FrameworkElement BuildTransactionRow(Transaction transaction, DataTemplate avatarTemplate)
    {
        var grid = new Grid { Padding = new Thickness(0, 10, 0, 10), ColumnSpacing = 14 };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var avatar = new ContentControl { ContentTemplate = avatarTemplate, Content = transaction.Category, IsTabStop = false };

        var textPanel = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        textPanel.Children.Add(new TextBlock { Text = transaction.Category?.Name ?? "Uncategorized", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
        if (!string.IsNullOrWhiteSpace(transaction.Note))
        {
            textPanel.Children.Add(new TextBlock
            {
                Text = transaction.Note,
                Style = (Style)Application.Current.Resources["AppMutedTextStyle"],
                FontSize = 12,
                TextTrimming = TextTrimming.CharacterEllipsis
            });
        }

        var dateText = new TextBlock
        {
            Text = transaction.Date.ToString("d"),
            VerticalAlignment = VerticalAlignment.Center,
            Style = (Style)Application.Current.Resources["AppMutedTextStyle"],
            FontSize = 13,
            Margin = new Thickness(0, 0, 16, 0)
        };

        var amountBrush = (Brush)Application.Current.Resources[
            transaction.Type == TransactionType.Income ? "AppIncomeBrush" : "AppExpenseBrush"];
        var amountText = new TextBlock
        {
            Text = _currencyService.Format(transaction.Amount),
            VerticalAlignment = VerticalAlignment.Center,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = amountBrush
        };

        Grid.SetColumn(avatar, 0);
        Grid.SetColumn(textPanel, 1);
        Grid.SetColumn(dateText, 2);
        Grid.SetColumn(amountText, 3);
        grid.Children.Add(avatar);
        grid.Children.Add(textPanel);
        grid.Children.Add(dateText);
        grid.Children.Add(amountText);
        return grid;
    }

    private static string WalletGlyph(WalletType type) => type switch
    {
        WalletType.Cash => "",
        WalletType.Bank => "",
        WalletType.CreditCard => "",
        WalletType.MobileBanking => "",
        _ => ""
    };

    private static Color HexToColor(string hex, byte alpha)
    {
        hex = hex.TrimStart('#');
        try
        {
            var r = System.Convert.ToByte(hex.Substring(0, 2), 16);
            var g = System.Convert.ToByte(hex.Substring(2, 2), 16);
            var b = System.Convert.ToByte(hex.Substring(4, 2), 16);
            return Color.FromArgb(alpha, r, g, b);
        }
        catch
        {
            return Color.FromArgb(alpha, 128, 128, 128);
        }
    }
}
