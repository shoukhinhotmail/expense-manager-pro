using ExpenseManager.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace ExpenseManager.App.Views;

public sealed partial class AiInsightsPage : Page
{
    public AiInsightsViewModel ViewModel { get; }

    public AiInsightsPage()
    {
        ViewModel = App.Host.Services.GetRequiredService<AiInsightsViewModel>();
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ViewModel.RefreshModelStatus();
    }

    private async void DownloadButton_Click(object sender, RoutedEventArgs e) =>
        await ViewModel.DownloadModelCommand.ExecuteAsync(null);

    private void CancelDownloadButton_Click(object sender, RoutedEventArgs e) =>
        ViewModel.CancelDownloadCommand.Execute(null);

    private async void GenerateButton_Click(object sender, RoutedEventArgs e) =>
        await ViewModel.GenerateInsightsCommand.ExecuteAsync(null);

    private async void DeleteModelButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = "Remove AI model?",
            Content = "This deletes the downloaded model file (~1.7 GB) from your disk. You can download it again anytime.",
            PrimaryButtonText = "Remove",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary) return;

        ViewModel.DeleteModelCommand.Execute(null);
    }
}
