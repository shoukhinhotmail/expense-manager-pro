using Microsoft.UI.Xaml;

namespace ExpenseManager.App.Services;

public class ThemeService(SettingsService settings)
{
    public event EventHandler<ElementTheme>? ThemeChanged;

    public ElementTheme CurrentTheme
    {
        get => settings.Current.Theme switch
        {
            "Light" => ElementTheme.Light,
            "Dark" => ElementTheme.Dark,
            _ => ElementTheme.Default
        };
    }

    public void ApplyTheme(FrameworkElement root, ElementTheme theme)
    {
        root.RequestedTheme = theme;
        settings.Current.Theme = theme switch
        {
            ElementTheme.Light => "Light",
            ElementTheme.Dark => "Dark",
            _ => "Default"
        };
        settings.Save();
        ThemeChanged?.Invoke(this, theme);
    }

    /// <summary>Resolves ElementTheme.Default against the OS setting so callers that need a
    /// concrete Light/Dark answer (e.g. title bar button colors) don't have to special-case it.</summary>
    public static ElementTheme ResolveActualTheme(FrameworkElement root) =>
        root.ActualTheme == ElementTheme.Dark ? ElementTheme.Dark : ElementTheme.Light;
}
