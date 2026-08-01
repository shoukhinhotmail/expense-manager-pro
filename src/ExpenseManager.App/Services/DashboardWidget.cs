namespace ExpenseManager.App.Services;

public enum DashboardWidget
{
    Summary,
    Wallets,
    Charts,
    RecentTransactions
}

public class DashboardWidgetConfig
{
    public DashboardWidget Widget { get; set; }
    public bool IsVisible { get; set; } = true;
}
