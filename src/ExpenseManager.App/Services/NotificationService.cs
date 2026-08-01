using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;

namespace ExpenseManager.App.Services;

public class NotificationService
{
    private bool _registered;

    public void Register()
    {
        try
        {
            AppNotificationManager.Default.Register();
            _registered = true;
        }
        catch
        {
            // Toasts are a nice-to-have; if registration fails on this machine/setup, the app
            // should keep working silently without them rather than crash.
            _registered = false;
        }
    }

    public void Show(string title, string body)
    {
        if (!_registered) return;

        try
        {
            var notification = new AppNotificationBuilder()
                .AddText(title)
                .AddText(body)
                .BuildNotification();
            AppNotificationManager.Default.Show(notification);
        }
        catch
        {
            // Non-fatal — see Register().
        }
    }
}
