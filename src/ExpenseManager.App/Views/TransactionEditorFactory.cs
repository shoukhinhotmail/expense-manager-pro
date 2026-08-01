using ExpenseManager.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace ExpenseManager.App.Views;

/// <summary>Builds the Add/Edit transaction form shown inside a ContentDialog. Built in code
/// (rather than XAML) so the same form can be reused for both the Expenses and Income pages.</summary>
internal static class TransactionEditorFactory
{
    private static readonly string[] PresetColors =
    [
        "#EF4444", "#F97316", "#F59E0B", "#22C55E", "#14B8A6",
        "#3B82F6", "#6366F1", "#A855F7", "#EC4899", "#6B7280"
    ];

    public static FrameworkElement BuildContent(TransactionEditViewModel vm)
    {
        var panel = new StackPanel { Spacing = 12, MinWidth = 340 };

        var errorText = new TextBlock
        {
            Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red),
            TextWrapping = TextWrapping.Wrap
        };
        errorText.SetBinding(TextBlock.TextProperty, new Binding { Source = vm, Path = new PropertyPath(nameof(vm.ErrorMessage)), Mode = BindingMode.OneWay });
        panel.Children.Add(errorText);

        var amountBox = new TextBox { Header = "Amount", PlaceholderText = "0.00", InputScope = new InputScope() };
        amountBox.InputScope.Names.Add(new InputScopeName(InputScopeNameValue.CurrencyAmountAndSymbol));
        amountBox.SetBinding(TextBox.TextProperty, new Binding { Source = vm, Path = new PropertyPath(nameof(vm.AmountText)), Mode = BindingMode.TwoWay });
        panel.Children.Add(amountBox);

        var walletBox = new ComboBox
        {
            Header = "Wallet",
            DisplayMemberPath = nameof(Core.Entities.Wallet.Name),
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        walletBox.SetBinding(ItemsControl.ItemsSourceProperty, new Binding { Source = vm, Path = new PropertyPath(nameof(vm.Wallets)), Mode = BindingMode.OneWay });
        walletBox.SetBinding(Selector.SelectedItemProperty, new Binding { Source = vm, Path = new PropertyPath(nameof(vm.SelectedWallet)), Mode = BindingMode.TwoWay });
        panel.Children.Add(walletBox);

        var categoryRow = new Grid { ColumnSpacing = 8 };
        categoryRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        categoryRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var categoryBox = new ComboBox
        {
            Header = "Category",
            DisplayMemberPath = nameof(Core.Entities.Category.Name),
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        categoryBox.SetBinding(ItemsControl.ItemsSourceProperty, new Binding { Source = vm, Path = new PropertyPath(nameof(vm.Categories)), Mode = BindingMode.OneWay });
        categoryBox.SetBinding(Selector.SelectedItemProperty, new Binding { Source = vm, Path = new PropertyPath(nameof(vm.SelectedCategory)), Mode = BindingMode.TwoWay });
        Grid.SetColumn(categoryBox, 0);
        categoryRow.Children.Add(categoryBox);

        var addCategoryButton = new Button
        {
            Content = new FontIcon { Glyph = "", FontSize = 12 },
            VerticalAlignment = VerticalAlignment.Bottom,
            Height = 32
        };
        ToolTipService.SetToolTip(addCategoryButton, "Add new category");
        addCategoryButton.Flyout = BuildAddCategoryFlyout(vm);
        Grid.SetColumn(addCategoryButton, 1);
        categoryRow.Children.Add(addCategoryButton);

        panel.Children.Add(categoryRow);

        var datePicker = new CalendarDatePicker { Header = "Date", HorizontalAlignment = HorizontalAlignment.Stretch };
        datePicker.SetBinding(CalendarDatePicker.DateProperty, new Binding { Source = vm, Path = new PropertyPath(nameof(vm.Date)), Mode = BindingMode.TwoWay });
        panel.Children.Add(datePicker);

        var noteBox = new TextBox { Header = "Note (optional)", AcceptsReturn = false };
        noteBox.SetBinding(TextBox.TextProperty, new Binding { Source = vm, Path = new PropertyPath(nameof(vm.Note)), Mode = BindingMode.TwoWay });
        panel.Children.Add(noteBox);

        return panel;
    }

    private static Flyout BuildAddCategoryFlyout(TransactionEditViewModel vm)
    {
        var nameBox = new TextBox { Header = "New category name", PlaceholderText = "e.g. Subscriptions" };
        var selectedColor = PresetColors[0];
        var swatchButtons = new List<Button>();
        var swatchPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Margin = new Thickness(0, 8, 0, 0) };

        foreach (var hex in PresetColors)
        {
            var swatch = new Button
            {
                Width = 26,
                Height = 26,
                CornerRadius = new CornerRadius(13),
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

        var addButton = new Button
        {
            Content = "Add category",
            Style = (Style)Application.Current.Resources["AccentButtonStyle"],
            Margin = new Thickness(0, 12, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        var content = new StackPanel { Spacing = 0, Width = 260 };
        content.Children.Add(nameBox);
        content.Children.Add(swatchPanel);
        content.Children.Add(addButton);

        var flyout = new Flyout { Content = content };

        addButton.Click += async (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(nameBox.Text)) return;
            await vm.AddCategoryAsync(nameBox.Text.Trim(), selectedColor);
            nameBox.Text = string.Empty;
            flyout.Hide();
        };

        return flyout;
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
