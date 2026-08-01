using ExpenseManager.Core.Currency;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace ExpenseManager.App.Controls;

public sealed partial class CurrencyPickerControl : UserControl
{
    private static readonly List<string> AllDisplayItems =
        CurrencyCatalog.All.Select(DisplayText).ToList();

    public static readonly DependencyProperty SelectedCodeProperty = DependencyProperty.Register(
        nameof(SelectedCode), typeof(string), typeof(CurrencyPickerControl),
        new PropertyMetadata(null, OnSelectedCodeChanged));

    public event EventHandler<string>? CurrencySelected;

    public string? SelectedCode
    {
        get => (string?)GetValue(SelectedCodeProperty);
        set => SetValue(SelectedCodeProperty, value);
    }

    public CurrencyPickerControl()
    {
        InitializeComponent();
        SuggestBox.ItemsSource = AllDisplayItems;
    }

    private static void OnSelectedCodeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (CurrencyPickerControl)d;
        var currency = CurrencyCatalog.Find(e.NewValue as string);
        if (currency is not null)
            control.SuggestBox.Text = DisplayText(currency);
    }

    private void ShowFullListIfClosed()
    {
        if (SuggestBox.IsSuggestionListOpen) return;
        SuggestBox.ItemsSource = AllDisplayItems;
        DispatcherQueue.TryEnqueue(() => SuggestBox.IsSuggestionListOpen = true);
    }

    private void SuggestBox_GotFocus(object sender, RoutedEventArgs e) => ShowFullListIfClosed();

    private void SuggestBox_Tapped(object sender, TappedRoutedEventArgs e) => ShowFullListIfClosed();

    private void SuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        // Fires when the user clicks the search icon or presses Enter. If there's no text yet,
        // the icon click is really just "show me the list" rather than an actual search.
        if (string.IsNullOrWhiteSpace(sender.Text))
            ShowFullListIfClosed();
    }

    private void SuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput) return;

        var query = sender.Text.Trim();
        var matches = string.IsNullOrEmpty(query)
            ? CurrencyCatalog.All
            : CurrencyCatalog.All.Where(c =>
                c.Code.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                c.Name.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();

        sender.ItemsSource = matches.Take(30).Select(DisplayText).ToList();
        sender.IsSuggestionListOpen = true;
    }

    private void SuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        if (args.SelectedItem is not string text) return;
        var code = text.Split(" — ")[0];
        SelectedCode = code;
        CurrencySelected?.Invoke(this, code);
    }

    private static string DisplayText(CurrencyInfo c) => $"{c.Code} — {c.Name} ({c.Symbol})";
}
