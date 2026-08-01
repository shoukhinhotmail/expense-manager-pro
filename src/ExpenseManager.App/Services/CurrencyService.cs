using ExpenseManager.Core.Currency;

namespace ExpenseManager.App.Services;

public class CurrencyService(SettingsService settings)
{
    public event EventHandler? CurrencyChanged;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(settings.Current.CurrencyCode);

    public CurrencyInfo Current =>
        CurrencyCatalog.Find(settings.Current.CurrencyCode) ?? CurrencyCatalog.Default;

    public void SetCurrency(string code)
    {
        settings.Current.CurrencyCode = code;
        settings.Save();
        CurrencyChanged?.Invoke(this, EventArgs.Empty);
    }

    public string Format(decimal amount)
    {
        var currency = Current;
        var formatted = Math.Abs(amount).ToString("N" + currency.DecimalDigits);
        var sign = amount < 0 ? "-" : "";
        return $"{sign}{currency.Symbol}{formatted}";
    }

    /// <summary>Same formatting but with the ISO currency code instead of the symbol (e.g. "BDT
    /// 1,234.56" instead of "৳1,234.56"). Use this anywhere text is rendered with a single fixed
    /// font that can't be relied on to have glyph coverage for every script — PDF export, for
    /// instance, has no OS-level font fallback the way live UI text does.</summary>
    public string FormatPlain(decimal amount)
    {
        var currency = Current;
        var formatted = Math.Abs(amount).ToString("N" + currency.DecimalDigits);
        var sign = amount < 0 ? "-" : "";
        return $"{sign}{currency.Code} {formatted}";
    }
}
