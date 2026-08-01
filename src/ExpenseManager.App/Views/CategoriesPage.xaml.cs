using ExpenseManager.App.ViewModels;
using ExpenseManager.Core.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

namespace ExpenseManager.App.Views;

public sealed partial class CategoriesPage : Page
{
    private static readonly string[] PresetColors =
    [
        "#EF4444", "#F97316", "#F59E0B", "#22C55E", "#14B8A6",
        "#3B82F6", "#6366F1", "#A855F7", "#EC4899", "#6B7280"
    ];

    public CategoriesViewModel ViewModel { get; }

    public CategoriesPage()
    {
        ViewModel = App.Host.Services.GetRequiredService<CategoriesViewModel>();
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await ViewModel.LoadAsync();
    }

    private async void AddExpenseCategory_Click(object sender, RoutedEventArgs e) =>
        await ShowEditorAsync(TransactionType.Expense, existing: null);

    private async void AddIncomeCategory_Click(object sender, RoutedEventArgs e) =>
        await ShowEditorAsync(TransactionType.Income, existing: null);

    private async void EditCategory_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem { Tag: Category category })
            await ShowEditorAsync(category.Type, category);
    }

    private async void DeleteCategory_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem { Tag: Category category })
            await ViewModel.DeleteCommand.ExecuteAsync(category);
    }

    private async Task ShowEditorAsync(TransactionType type, Category? existing)
    {
        var nameBox = new TextBox
        {
            Header = "Name",
            Text = existing?.Name ?? string.Empty
        };

        var selectedColor = existing?.Color ?? PresetColors[0];
        var swatchButtons = new List<Button>();

        var swatchPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };

        foreach (var hex in PresetColors)
        {
            var isSelected = hex == selectedColor;
            var swatch = new Button
            {
                Width = 32,
                Height = 32,
                CornerRadius = new CornerRadius(16),
                Padding = new Thickness(0),
                Tag = hex,
                Background = new SolidColorBrush(HexToColor(hex)),
                BorderThickness = new Thickness(isSelected ? 3 : 0),
                BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.White)
            };
            swatch.Click += (s, _) =>
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
        panel.Children.Add(new TextBlock { Text = "Color", FontSize = 12, Opacity = 0.7 });
        panel.Children.Add(swatchPanel);

        var dialog = new ContentDialog
        {
            Title = existing is null ? "Add category" : "Edit category",
            PrimaryButtonText = "Save",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            Content = panel,
            XamlRoot = XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary) return;
        if (string.IsNullOrWhiteSpace(nameBox.Text)) return;

        var category = existing ?? new Category { Type = type, Glyph = "" };
        category.Name = nameBox.Text.Trim();
        category.Color = selectedColor;

        await ViewModel.SaveAsync(category);
        await ViewModel.LoadAsync();
    }

    private static Windows.UI.Color HexToColor(string hex)
    {
        hex = hex.TrimStart('#');
        var r = System.Convert.ToByte(hex.Substring(0, 2), 16);
        var g = System.Convert.ToByte(hex.Substring(2, 2), 16);
        var b = System.Convert.ToByte(hex.Substring(4, 2), 16);
        return Windows.UI.Color.FromArgb(255, r, g, b);
    }
}
