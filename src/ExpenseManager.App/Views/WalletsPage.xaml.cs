using ExpenseManager.App.ViewModels;
using ExpenseManager.Core.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

namespace ExpenseManager.App.Views;

public sealed partial class WalletsPage : Page
{
    private static readonly string[] PresetColors =
    [
        "#EF4444", "#F97316", "#F59E0B", "#22C55E", "#14B8A6",
        "#3B82F6", "#6366F1", "#A855F7", "#EC4899", "#6B7280"
    ];

    public WalletsViewModel ViewModel { get; }

    public WalletsPage()
    {
        ViewModel = App.Host.Services.GetRequiredService<WalletsViewModel>();
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await ViewModel.LoadAsync();
    }

    private async void AddButton_Click(object sender, RoutedEventArgs e) => await ShowEditorAsync(existing: null);

    private async void EditButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem { Tag: Wallet wallet })
            await ShowEditorAsync(wallet);
    }

    private async void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuFlyoutItem { Tag: Wallet wallet }) return;

        if (wallet.IsSystem)
        {
            var info = new ContentDialog
            {
                Title = "Can't delete this wallet",
                Content = "This is one of the starter wallets. You can rename or recolor it, but it can't be deleted.",
                CloseButtonText = "OK",
                XamlRoot = XamlRoot
            };
            await info.ShowAsync();
            return;
        }

        await ViewModel.DeleteCommand.ExecuteAsync(wallet);
    }

    private async Task ShowEditorAsync(Wallet? existing)
    {
        var nameBox = new TextBox { Header = "Name", Text = existing?.Name ?? string.Empty };

        var typeBox = new ComboBox { Header = "Type", HorizontalAlignment = HorizontalAlignment.Stretch };
        typeBox.Items.Add(new ComboBoxItem { Content = "Cash", Tag = WalletType.Cash });
        typeBox.Items.Add(new ComboBoxItem { Content = "Bank", Tag = WalletType.Bank });
        typeBox.Items.Add(new ComboBoxItem { Content = "Credit Card", Tag = WalletType.CreditCard });
        typeBox.Items.Add(new ComboBoxItem { Content = "Mobile Banking", Tag = WalletType.MobileBanking });
        typeBox.Items.Add(new ComboBoxItem { Content = "Other", Tag = WalletType.Other });
        typeBox.SelectedIndex = existing is null ? 0 : (int)existing.Type;

        var balanceBox = new TextBox
        {
            Header = existing is null ? "Starting balance" : "Starting balance (before tracked transactions)",
            Text = (existing?.InitialBalance ?? 0m).ToString("0.##")
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
        panel.Children.Add(typeBox);
        panel.Children.Add(balanceBox);
        panel.Children.Add(new TextBlock { Text = "Color", FontSize = 12, Opacity = 0.7 });
        panel.Children.Add(swatchPanel);

        var dialog = new ContentDialog
        {
            Title = existing is null ? "Add wallet" : "Edit wallet",
            PrimaryButtonText = "Save",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            Content = panel,
            XamlRoot = XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary) return;
        if (string.IsNullOrWhiteSpace(nameBox.Text)) return;
        if (!decimal.TryParse(balanceBox.Text, out var initialBalance)) initialBalance = 0m;

        var wallet = existing ?? new Wallet();
        wallet.Name = nameBox.Text.Trim();
        wallet.Type = (WalletType)((ComboBoxItem)typeBox.SelectedItem).Tag;
        wallet.Color = selectedColor;
        wallet.InitialBalance = initialBalance;

        await ViewModel.SaveAsync(wallet);
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
