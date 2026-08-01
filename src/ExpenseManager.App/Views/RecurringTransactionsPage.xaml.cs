using ExpenseManager.App.ViewModels;
using ExpenseManager.Core.Entities;
using ExpenseManager.Core.Repositories;
using ExpenseManager.App.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;

namespace ExpenseManager.App.Views;

public sealed partial class RecurringTransactionsPage : Page
{
    public RecurringTransactionsViewModel ViewModel { get; }

    public RecurringTransactionsPage()
    {
        ViewModel = App.Host.Services.GetRequiredService<RecurringTransactionsViewModel>();
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
        if (sender is MenuFlyoutItem { Tag: RecurringTransaction recurring })
            await ShowEditorAsync(recurring);
    }

    private async void ToggleActiveButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleMenuFlyoutItem { Tag: RecurringTransaction recurring })
            await ViewModel.ToggleActiveCommand.ExecuteAsync(recurring);
    }

    private async void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuFlyoutItem { Tag: RecurringTransaction recurring }) return;

        var transactionRepository = App.Host.Services.GetRequiredService<ITransactionRepository>();
        var generatedCount = await transactionRepository.CountByRecurringTransactionIdAsync(recurring.Id);

        if (generatedCount == 0)
        {
            await ViewModel.DeleteCommand.ExecuteAsync(recurring);
            return;
        }

        var dialog = new ContentDialog
        {
            Title = "Delete recurring transaction?",
            Content = $"This schedule has already created {generatedCount} transaction{(generatedCount == 1 ? "" : "s")}. " +
                      "Do you want to keep them as regular transactions, or remove them too?",
            PrimaryButtonText = "Keep transactions",
            SecondaryButtonText = "Delete both",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.None) return; // Cancel

        if (result == ContentDialogResult.Secondary)
            await transactionRepository.DeleteByRecurringTransactionIdAsync(recurring.Id);

        await ViewModel.DeleteCommand.ExecuteAsync(recurring);
    }

    private async Task ShowEditorAsync(RecurringTransaction? existing)
    {
        var categoryRepository = App.Host.Services.GetRequiredService<ICategoryRepository>();
        var walletRepository = App.Host.Services.GetRequiredService<IWalletRepository>();

        var typeBox = new ComboBox { Header = "Type", HorizontalAlignment = HorizontalAlignment.Stretch };
        typeBox.Items.Add(new ComboBoxItem { Content = "Expense", Tag = TransactionType.Expense });
        typeBox.Items.Add(new ComboBoxItem { Content = "Income", Tag = TransactionType.Income });
        typeBox.SelectedIndex = existing is null || existing.Type == TransactionType.Expense ? 0 : 1;

        var amountBox = new TextBox { Header = "Amount", Text = existing?.Amount.ToString("0.##") ?? string.Empty };

        var categoryBox = new ComboBox { Header = "Category", DisplayMemberPath = nameof(Category.Name), HorizontalAlignment = HorizontalAlignment.Stretch };
        var walletBox = new ComboBox { Header = "Wallet", DisplayMemberPath = nameof(Wallet.Name), HorizontalAlignment = HorizontalAlignment.Stretch };

        async Task ReloadCategoriesAsync()
        {
            var type = (TransactionType)((ComboBoxItem)typeBox.SelectedItem).Tag;
            var categories = await categoryRepository.GetAllAsync(type);
            categoryBox.ItemsSource = categories;
            categoryBox.SelectedItem = existing is not null
                ? categories.FirstOrDefault(c => c.Id == existing.CategoryId)
                : categories.FirstOrDefault();
        }
        Task? pendingCategoryReload = null;
        typeBox.SelectionChanged += (_, _) => pendingCategoryReload = ReloadCategoriesAsync();
        await ReloadCategoriesAsync();

        var wallets = await walletRepository.GetAllAsync();
        walletBox.ItemsSource = wallets;
        walletBox.SelectedItem = existing is not null
            ? wallets.FirstOrDefault(w => w.Id == existing.WalletId)
            : wallets.FirstOrDefault(w => w.IsDefault) ?? wallets.FirstOrDefault();

        var frequencyBox = new ComboBox { Header = "Repeats", HorizontalAlignment = HorizontalAlignment.Stretch };
        foreach (var freq in Enum.GetValues<RecurrenceFrequency>())
            frequencyBox.Items.Add(new ComboBoxItem { Content = freq.ToString(), Tag = freq });
        frequencyBox.SelectedIndex = existing is null ? 3 : (int)existing.Frequency; // default Monthly

        var startDatePicker = new CalendarDatePicker
        {
            Header = existing is null ? "Starts on" : "Started on",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Date = existing?.StartDate ?? DateTime.Today,
            IsEnabled = existing is null
        };

        var hasEndDateBox = new CheckBox { Content = "Ends on a specific date", IsChecked = existing?.EndDate is not null };
        var endDatePicker = new CalendarDatePicker
        {
            Header = "End date",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Date = existing?.EndDate ?? DateTime.Today.AddMonths(1),
            Visibility = existing?.EndDate is not null ? Visibility.Visible : Visibility.Collapsed
        };
        hasEndDateBox.Checked += (_, _) =>
        {
            endDatePicker.Visibility = Visibility.Visible;
            endDatePicker.Date ??= DateTime.Today.AddMonths(1);
        };
        hasEndDateBox.Unchecked += (_, _) => endDatePicker.Visibility = Visibility.Collapsed;

        var reminderBox = new ComboBox { Header = "Remind me", HorizontalAlignment = HorizontalAlignment.Stretch };
        reminderBox.Items.Add(new ComboBoxItem { Content = "On the due date", Tag = 0 });
        reminderBox.Items.Add(new ComboBoxItem { Content = "1 day before", Tag = 1 });
        reminderBox.Items.Add(new ComboBoxItem { Content = "3 days before", Tag = 3 });
        reminderBox.Items.Add(new ComboBoxItem { Content = "7 days before", Tag = 7 });
        reminderBox.SelectedIndex = existing?.ReminderDaysBefore switch { 1 => 1, 3 => 2, 7 => 3, _ => 0 };

        var noteBox = new TextBox { Header = "Note (optional)", Text = existing?.Note ?? string.Empty };

        var panel = new StackPanel { Spacing = 12, MinWidth = 340 };
        panel.Children.Add(typeBox);
        panel.Children.Add(amountBox);
        panel.Children.Add(categoryBox);
        panel.Children.Add(walletBox);
        panel.Children.Add(frequencyBox);
        panel.Children.Add(startDatePicker);
        panel.Children.Add(hasEndDateBox);
        panel.Children.Add(endDatePicker);
        panel.Children.Add(reminderBox);
        panel.Children.Add(noteBox);

        var scrollable = new ScrollViewer { Content = panel, MaxHeight = 460 };

        var dialog = new ContentDialog
        {
            Title = existing is null ? "Add recurring transaction" : "Edit recurring transaction",
            PrimaryButtonText = "Save",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            Content = scrollable,
            XamlRoot = XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary) return;

        // Make sure any in-flight category reload (from switching the Type dropdown) is done
        // before we touch the database — otherwise two calls can race on the same DbContext and
        // the save silently corrupts the follow-up reload without surfacing an error.
        if (pendingCategoryReload is not null)
            await pendingCategoryReload;

        if (!decimal.TryParse(amountBox.Text, out var amount) || amount <= 0) return;
        if (categoryBox.SelectedItem is not Category category) return;
        if (walletBox.SelectedItem is not Wallet wallet) return;

        var recurring = existing ?? new RecurringTransaction { StartDate = startDatePicker.Date?.Date ?? DateTime.Today };
        recurring.Type = (TransactionType)((ComboBoxItem)typeBox.SelectedItem).Tag;
        recurring.Amount = amount;
        recurring.CategoryId = category.Id;
        recurring.WalletId = wallet.Id;
        recurring.Frequency = (RecurrenceFrequency)((ComboBoxItem)frequencyBox.SelectedItem).Tag;
        recurring.EndDate = hasEndDateBox.IsChecked == true ? endDatePicker.Date?.Date : null;
        recurring.ReminderDaysBefore = (int)((ComboBoxItem)reminderBox.SelectedItem).Tag;
        recurring.Note = string.IsNullOrWhiteSpace(noteBox.Text) ? null : noteBox.Text.Trim();

        if (existing is null)
            recurring.NextDueDate = recurring.StartDate;

        await ViewModel.SaveAsync(recurring);

        // Post any occurrences that are already due right away, rather than making the user
        // restart the app before they see the transactions this schedule generates.
        var recurringService = App.Host.Services.GetRequiredService<RecurringService>();
        var generated = await recurringService.ProcessDueAsync();

        await ViewModel.LoadAsync();

        if (generated.Count > 0)
        {
            await ShowMessageAsync(
                "Recurring transaction saved",
                generated.Count == 1
                    ? $"1 transaction was posted right away: {generated[0]}."
                    : $"{generated.Count} transactions were posted right away: {string.Join(", ", generated)}.");
        }
    }

    private async Task ShowMessageAsync(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = XamlRoot
        };
        await dialog.ShowAsync();
    }
}
