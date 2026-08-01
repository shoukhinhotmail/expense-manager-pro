using ExpenseManager.App.ViewModels;
using ExpenseManager.Core.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

namespace ExpenseManager.App.Views;

public sealed partial class GoalsPage : Page
{
    private static readonly string[] PresetColors =
    [
        "#EF4444", "#F97316", "#F59E0B", "#22C55E", "#14B8A6",
        "#3B82F6", "#6366F1", "#A855F7", "#EC4899", "#6B7280"
    ];

    public GoalsViewModel ViewModel { get; }

    public GoalsPage()
    {
        ViewModel = App.Host.Services.GetRequiredService<GoalsViewModel>();
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await ViewModel.LoadAsync();
    }

    // ===================== Savings goals =====================

    private async void AddGoalButton_Click(object sender, RoutedEventArgs e) => await ShowGoalEditorAsync(existing: null);

    private async void EditGoalButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem { Tag: SavingsGoal goal })
            await ShowGoalEditorAsync(goal);
    }

    private async void DeleteGoalButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem { Tag: SavingsGoal goal })
            await ViewModel.DeleteGoalCommand.ExecuteAsync(goal);
    }

    private async void AddContributionButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuFlyoutItem { Tag: SavingsGoalRow row }) return;

        var amountBox = new TextBox
        {
            Header = "Amount (use a negative number to withdraw)",
            PlaceholderText = "0.00",
            InputScope = new InputScope()
        };
        amountBox.InputScope.Names.Add(new InputScopeName(InputScopeNameValue.CurrencyAmountAndSymbol));

        var dialog = new ContentDialog
        {
            Title = $"Update contribution — {row.Goal.Name}",
            PrimaryButtonText = "Save",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            Content = amountBox,
            XamlRoot = XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary) return;
        if (!decimal.TryParse(amountBox.Text, out var amount) || amount == 0) return;

        await ViewModel.AddContributionAsync(row.Goal.Id, amount);
    }

    private async Task ShowGoalEditorAsync(SavingsGoal? existing)
    {
        var nameBox = new TextBox { Header = "Name", Text = existing?.Name ?? string.Empty, PlaceholderText = "e.g. Emergency fund" };

        var targetBox = new TextBox
        {
            Header = "Target amount",
            PlaceholderText = "0.00",
            Text = existing is null ? string.Empty : existing.TargetAmount.ToString("0.##")
        };

        var currentBox = new TextBox
        {
            Header = "Current amount",
            PlaceholderText = "0.00",
            Text = (existing?.CurrentAmount ?? 0m).ToString("0.##")
        };

        var dateBox = new CalendarDatePicker
        {
            Header = "Target date (optional)",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Date = existing?.TargetDate is { } d ? new DateTimeOffset(d) : null
        };

        var selectedColor = existing?.Color ?? PresetColors[0];
        var swatchButtons = new List<Button>();
        var swatchPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };

        foreach (var hex in PresetColors)
        {
            var swatch = new Button
            {
                Width = 28,
                Height = 28,
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(0),
                Tag = hex,
                Background = new SolidColorBrush(HexToColor(hex)),
                BorderThickness = new Thickness(hex == selectedColor ? 3 : 0),
                BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.White)
            };
            swatch.Click += (_, _) =>
            {
                selectedColor = hex;
                foreach (var b in swatchButtons)
                    b.BorderThickness = new Thickness(Equals(b.Tag, selectedColor) ? 3 : 0);
            };
            swatchButtons.Add(swatch);
            swatchPanel.Children.Add(swatch);
        }

        var panel = new StackPanel { Spacing = 12, MinWidth = 340 };
        panel.Children.Add(nameBox);
        panel.Children.Add(targetBox);
        panel.Children.Add(currentBox);
        panel.Children.Add(dateBox);
        panel.Children.Add(new TextBlock { Text = "Color", FontSize = 12, Opacity = 0.7 });
        panel.Children.Add(swatchPanel);

        var dialog = new ContentDialog
        {
            Title = existing is null ? "Add savings goal" : "Edit savings goal",
            PrimaryButtonText = "Save",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            Content = panel,
            XamlRoot = XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary) return;
        if (string.IsNullOrWhiteSpace(nameBox.Text)) return;
        if (!decimal.TryParse(targetBox.Text, out var target) || target <= 0) return;
        if (!decimal.TryParse(currentBox.Text, out var current)) current = 0m;

        var goal = existing ?? new SavingsGoal();
        goal.Name = nameBox.Text.Trim();
        goal.TargetAmount = target;
        goal.CurrentAmount = current;
        goal.TargetDate = dateBox.Date?.DateTime;
        goal.Color = selectedColor;

        await ViewModel.SaveGoalAsync(goal);
        await ViewModel.LoadAsync();
    }

    // ===================== Budget limits =====================

    private async void AddBudgetButton_Click(object sender, RoutedEventArgs e) => await ShowBudgetEditorAsync(existing: null);

    private async void EditBudgetButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem { Tag: BudgetLimit budget })
            await ShowBudgetEditorAsync(budget);
    }

    private async void DeleteBudgetButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem { Tag: BudgetLimit budget })
            await ViewModel.DeleteBudgetCommand.ExecuteAsync(budget);
    }

    private async Task ShowBudgetEditorAsync(BudgetLimit? existing)
    {
        if (ViewModel.ExpenseCategories.Count == 0)
        {
            var info = new ContentDialog
            {
                Title = "No expense categories",
                Content = "Add an expense category first, then come back to set a budget for it.",
                CloseButtonText = "OK",
                XamlRoot = XamlRoot
            };
            await info.ShowAsync();
            return;
        }

        var categoryBox = new ComboBox
        {
            Header = "Category",
            DisplayMemberPath = nameof(Category.Name),
            ItemsSource = ViewModel.ExpenseCategories,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        categoryBox.SelectedItem = existing is null
            ? ViewModel.ExpenseCategories[0]
            : ViewModel.ExpenseCategories.FirstOrDefault(c => c.Id == existing.CategoryId) ?? ViewModel.ExpenseCategories[0];

        var limitBox = new TextBox
        {
            Header = "Limit amount",
            PlaceholderText = "0.00",
            Text = existing is null ? string.Empty : existing.LimitAmount.ToString("0.##")
        };

        var periodBox = new ComboBox { Header = "Period", HorizontalAlignment = HorizontalAlignment.Stretch };
        periodBox.Items.Add(new ComboBoxItem { Content = "Monthly", Tag = BudgetPeriod.Monthly });
        periodBox.Items.Add(new ComboBoxItem { Content = "Weekly", Tag = BudgetPeriod.Weekly });
        periodBox.SelectedIndex = existing is { Period: BudgetPeriod.Weekly } ? 1 : 0;

        var panel = new StackPanel { Spacing = 12, MinWidth = 340 };
        panel.Children.Add(categoryBox);
        panel.Children.Add(limitBox);
        panel.Children.Add(periodBox);

        var dialog = new ContentDialog
        {
            Title = existing is null ? "Add budget" : "Edit budget",
            PrimaryButtonText = "Save",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            Content = panel,
            XamlRoot = XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary) return;
        if (categoryBox.SelectedItem is not Category category) return;
        if (!decimal.TryParse(limitBox.Text, out var limit) || limit <= 0) return;

        var budget = existing ?? new BudgetLimit();
        budget.CategoryId = category.Id;
        budget.LimitAmount = limit;
        budget.Period = (BudgetPeriod)((ComboBoxItem)periodBox.SelectedItem!).Tag;

        await ViewModel.SaveBudgetAsync(budget);
        await ViewModel.LoadAsync();
    }

    private static Windows.UI.Color HexToColor(string hex)
    {
        hex = hex.TrimStart('#');
        var r = Convert.ToByte(hex.Substring(0, 2), 16);
        var g = Convert.ToByte(hex.Substring(2, 2), 16);
        var b = Convert.ToByte(hex.Substring(4, 2), 16);
        return Windows.UI.Color.FromArgb(255, r, g, b);
    }
}
