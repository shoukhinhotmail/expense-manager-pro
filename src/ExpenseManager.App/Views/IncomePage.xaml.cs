using ExpenseManager.App.ViewModels;
using ExpenseManager.Core.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace ExpenseManager.App.Views;

public sealed partial class IncomePage : Page
{
    public IncomeViewModel ViewModel { get; }

    public IncomePage()
    {
        ViewModel = App.Host.Services.GetRequiredService<IncomeViewModel>();
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await ViewModel.LoadFiltersAsync();
        // Guarded by IsLoaded inside Filter_Changed, so these don't trigger a redundant
        // load — the explicit LoadAsync() below is the one that actually runs.
        CategoryFilterCombo.SelectedIndex = 0;
        WalletFilterCombo.SelectedIndex = 0;
        await ViewModel.LoadAsync();
    }

    private async void AddButton_Click(object sender, RoutedEventArgs e) => await ShowEditorAsync(existing: null);

    private async void EditButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem { Tag: Transaction transaction })
            await ShowEditorAsync(transaction);
    }

    private async void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem { Tag: Transaction transaction })
            await ViewModel.DeleteCommand.ExecuteAsync(transaction);
    }

    private async void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            await ViewModel.LoadAsync();
    }

    private void FiltersToggleButton_Click(object sender, RoutedEventArgs e) =>
        FiltersPanel.Visibility = FiltersPanel.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;

    private async void Filter_Changed(object sender, SelectionChangedEventArgs e) => await ApplyFiltersAsync();

    private async void DateFilter_Changed(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args) => await ApplyFiltersAsync();

    private async void AmountFilter_Changed(NumberBox sender, NumberBoxValueChangedEventArgs args) => await ApplyFiltersAsync();

    private async void ClearFilters_Click(object sender, RoutedEventArgs e)
    {
        CategoryFilterCombo.SelectedIndex = 0;
        WalletFilterCombo.SelectedIndex = 0;
        FromDatePicker.Date = null;
        ToDatePicker.Date = null;
        MinAmountBox.Value = double.NaN;
        MaxAmountBox.Value = double.NaN;
        await ViewModel.ClearFiltersCommand.ExecuteAsync(null);
    }

    private async Task ApplyFiltersAsync()
    {
        if (!IsLoaded) return;
        ViewModel.FilterCategory = CategoryFilterCombo.SelectedItem as Category;
        ViewModel.FilterWallet = WalletFilterCombo.SelectedItem as Wallet;
        ViewModel.FilterDateFrom = FromDatePicker.Date;
        ViewModel.FilterDateTo = ToDatePicker.Date;
        ViewModel.FilterMinAmount = MinAmountBox.Value;
        ViewModel.FilterMaxAmount = MaxAmountBox.Value;
        await ViewModel.LoadAsync();
    }

    private async Task ShowEditorAsync(Transaction? existing)
    {
        var editVm = App.Host.Services.GetRequiredService<TransactionEditViewModel>();
        await editVm.InitializeAsync(TransactionType.Income, existing);

        var dialog = new ContentDialog
        {
            Title = existing is null ? "Add income" : "Edit income",
            PrimaryButtonText = "Save",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            Content = TransactionEditorFactory.BuildContent(editVm),
            XamlRoot = XamlRoot
        };

        dialog.PrimaryButtonClick += (_, args) =>
        {
            var built = editVm.TryBuildTransaction();
            if (built is null)
            {
                args.Cancel = true;
            }
        };

        var result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary) return;

        var transaction = editVm.TryBuildTransaction();
        if (transaction is null) return;

        await ViewModel.SaveAsync(transaction);
        await ViewModel.LoadAsync();
    }
}
