using ExpenseManager.App.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;

namespace ExpenseManager.App;

public sealed partial class OnboardingWindow : Window
{
    private string? _selectedCode;

    public OnboardingWindow()
    {
        InitializeComponent();
        Title = "Welcome to Expense Manager Pro";
        AppWindow.SetIcon("Assets/app.ico");
    }

    private void Picker_CurrencySelected(object? sender, string code)
    {
        _selectedCode = code;
        ContinueButton.IsEnabled = true;
    }

    private void ContinueButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedCode is null) return;

        var currencyService = App.Host.Services.GetRequiredService<CurrencyService>();
        currencyService.SetCurrency(_selectedCode);

        App.LaunchMainWindow();
        Close();
    }
}
