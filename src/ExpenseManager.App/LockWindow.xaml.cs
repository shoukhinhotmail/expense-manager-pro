using ExpenseManager.App.Services;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using WinRT.Interop;

namespace ExpenseManager.App;

public sealed partial class LockWindow : Window
{
    private readonly LockService _lockService;

    public LockWindow(LockService lockService)
    {
        _lockService = lockService;
        InitializeComponent();
        Title = "Expense Manager Pro — Locked";

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        AppWindow.SetIcon("Assets/app.ico");

        const int width = 420;
        const int height = 480;

        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsResizable = false;
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
        }

        AppWindow.Resize(new Windows.Graphics.SizeInt32(width, height));

        var hwnd = WindowNative.GetWindowHandle(this);
        var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
        var displayArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Primary);
        var workArea = displayArea.WorkArea;
        AppWindow.Move(new Windows.Graphics.PointInt32(
            workArea.X + (workArea.Width - width) / 2,
            workArea.Y + (workArea.Height - height) / 2));

        Activated += LockWindow_Activated;
    }

    private bool _helloAttempted;

    private async void LockWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        if (_helloAttempted || !_lockService.IsWindowsHelloEnabled) return;
        _helloAttempted = true;

        if (!await _lockService.IsWindowsHelloAvailableAsync()) return;

        HelloButton.Visibility = Visibility.Visible;
        var verified = await _lockService.TryVerifyWithWindowsHelloAsync();
        if (verified) Unlock();
    }

    private async void HelloButton_Click(object sender, RoutedEventArgs e)
    {
        var verified = await _lockService.TryVerifyWithWindowsHelloAsync();
        if (verified)
            Unlock();
        else
            ShowError("Windows Hello verification failed. Enter your PIN instead.");
    }

    private void PinBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
            TrySubmitPin();
    }

    private void UnlockButton_Click(object sender, RoutedEventArgs e) => TrySubmitPin();

    private void TrySubmitPin()
    {
        if (_lockService.VerifyPin(PinBox.Password))
        {
            Unlock();
        }
        else
        {
            ShowError("Incorrect PIN. Try again.");
            PinBox.Password = string.Empty;
        }
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.Visibility = Visibility.Visible;
    }

    private void Unlock()
    {
        App.LaunchMainWindow();
        Close();
    }
}
