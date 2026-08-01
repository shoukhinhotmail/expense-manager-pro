namespace ExpenseManager.App.Services;

public class DashboardLayoutService(SettingsService settings)
{
    private static readonly DashboardWidget[] DefaultOrder =
    [
        DashboardWidget.Summary,
        DashboardWidget.Wallets,
        DashboardWidget.Charts,
        DashboardWidget.RecentTransactions
    ];

    /// <summary>Returns the widget layout in display order. Any widget missing from saved
    /// settings (e.g. added in a later app version) is appended at the end, visible by default,
    /// so old settings files stay forward-compatible.</summary>
    public List<DashboardWidgetConfig> GetLayout()
    {
        var saved = settings.Current.DashboardLayout;
        if (saved is null || saved.Count == 0)
            return DefaultOrder.Select(w => new DashboardWidgetConfig { Widget = w, IsVisible = true }).ToList();

        var result = new List<DashboardWidgetConfig>(saved);
        var known = saved.Select(c => c.Widget).ToHashSet();
        foreach (var widget in DefaultOrder)
        {
            if (!known.Contains(widget))
                result.Add(new DashboardWidgetConfig { Widget = widget, IsVisible = true });
        }
        return result;
    }

    public void SaveLayout(List<DashboardWidgetConfig> layout)
    {
        settings.Current.DashboardLayout = layout;
        settings.Save();
    }

    public static string DisplayName(DashboardWidget widget) => widget switch
    {
        DashboardWidget.Summary => "Summary cards (Income, Expenses, Balance)",
        DashboardWidget.Wallets => "Wallets",
        DashboardWidget.Charts => "Charts (Income vs. Expenses, Spending by category)",
        DashboardWidget.RecentTransactions => "Recent transactions",
        _ => widget.ToString()
    };
}
