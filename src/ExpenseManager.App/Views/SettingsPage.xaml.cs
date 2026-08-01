using System.Diagnostics;
using CommunityToolkit.WinUI.Controls;
using ExpenseManager.App.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace ExpenseManager.App.Views;

public sealed partial class SettingsPage : Page
{
    private readonly ThemeService _themeService;
    private readonly CurrencyService _currencyService;
    private readonly BackupService _backupService;
    private readonly LockService _lockService;
    private readonly GoogleDriveBackupService _googleDrive;
    private bool _isInitializing = true;

    public SettingsPage()
    {
        _themeService = App.Host.Services.GetRequiredService<ThemeService>();
        _currencyService = App.Host.Services.GetRequiredService<CurrencyService>();
        _backupService = App.Host.Services.GetRequiredService<BackupService>();
        _lockService = App.Host.Services.GetRequiredService<LockService>();
        _googleDrive = App.Host.Services.GetRequiredService<GoogleDriveBackupService>();
        InitializeComponent();

        var current = _themeService.CurrentTheme;
        foreach (var item in ThemeCombo.Items)
        {
            if (item is ComboBoxItem { Tag: string tag } cb && tag == current.ToString())
            {
                ThemeCombo.SelectedItem = cb;
                break;
            }
        }

        CurrencyPicker.SelectedCode = _currencyService.Current.Code;

        PinLockToggle.IsOn = _lockService.IsPinLockEnabled;
        WindowsHelloToggle.IsOn = _lockService.IsWindowsHelloEnabled;
        UpdateSecurityCardVisibility();

        GoogleDriveAutoBackupToggle.IsOn = App.Host.Services.GetRequiredService<SettingsService>().Current.IsGoogleDriveAutoBackupEnabled;

        _isInitializing = false;

        _ = InitializeGoogleDriveStatusAsync();
    }

    private async Task InitializeGoogleDriveStatusAsync()
    {
        if (_googleDrive.IsSignedIn || await _googleDrive.TryRestoreSessionAsync())
            await UpdateGoogleDriveConnectedUiAsync();
    }

    private async Task UpdateGoogleDriveConnectedUiAsync()
    {
        var email = await _googleDrive.GetSignedInEmailAsync();
        GoogleDriveStatusText.Text = email is not null ? $"Connected as {email}" : "Connected";
        GoogleDriveConnectButton.Content = "Disconnect";
        GoogleDriveAutoBackupCard.Visibility = Visibility.Visible;
        GoogleDriveBackupNowCard.Visibility = Visibility.Visible;
        GoogleDriveRestoreCard.Visibility = Visibility.Visible;
    }

    private void UpdateGoogleDriveDisconnectedUi()
    {
        GoogleDriveStatusText.Text = "Not connected";
        GoogleDriveConnectButton.Content = "Connect";
        GoogleDriveAutoBackupCard.Visibility = Visibility.Collapsed;
        GoogleDriveBackupNowCard.Visibility = Visibility.Collapsed;
        GoogleDriveRestoreCard.Visibility = Visibility.Collapsed;
    }

    private void UpdateSecurityCardVisibility()
    {
        var visible = _lockService.IsPinLockEnabled ? Visibility.Visible : Visibility.Collapsed;
        WindowsHelloCard.Visibility = visible;
        ChangePinCard.Visibility = visible;
    }

    private void ThemeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing) return;
        if (ThemeCombo.SelectedItem is not ComboBoxItem { Tag: string tag }) return;

        var theme = tag switch
        {
            "Light" => ElementTheme.Light,
            "Dark" => ElementTheme.Dark,
            _ => ElementTheme.Default
        };

        if (App.MainAppWindow?.Content is FrameworkElement root)
            _themeService.ApplyTheme(root, theme);
    }

    private void CurrencyPicker_CurrencySelected(object? sender, string code) =>
        _currencyService.SetCurrency(code);

    private async void BackupButton_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileSavePicker();
        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(App.MainAppWindow));
        picker.SuggestedStartLocation = PickerLocationId.Downloads;
        picker.FileTypeChoices.Add("Expense Manager Backup", [".embackup"]);
        picker.SuggestedFileName = $"ExpenseManagerPro-Backup-{DateTime.Now:yyyy-MM-dd}";

        var file = await picker.PickSaveFileAsync();
        if (file is null) return;

        try
        {
            await _backupService.BackupToAsync(file.Path);
            await ShowMessageAsync("Backup complete", $"Your data was backed up to:\n{file.Path}");
        }
        catch (Exception ex)
        {
            await ShowMessageAsync("Backup failed", ex.Message);
        }
    }

    private async void RestoreButton_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker();
        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(App.MainAppWindow));
        picker.FileTypeFilter.Add(".embackup");
        picker.FileTypeFilter.Add(".db");

        var file = await picker.PickSingleFileAsync();
        if (file is null) return;

        var confirm = new ContentDialog
        {
            Title = "Restore backup?",
            Content = "This replaces all current data with the backup file. This can't be undone. The app will restart afterwards.",
            PrimaryButtonText = "Restore",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot
        };

        if (await confirm.ShowAsync() != ContentDialogResult.Primary) return;

        try
        {
            _backupService.RestoreFrom(file.Path);

            var exePath = Process.GetCurrentProcess().MainModule?.FileName;
            if (exePath is not null)
                Process.Start(exePath);
            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            await ShowMessageAsync("Restore failed", ex.Message);
        }
    }

    private async void PinLockToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;

        if (PinLockToggle.IsOn)
        {
            var pin = await ShowSetPinDialogAsync("Set a PIN", "Choose a 4–6 digit PIN to lock the app.");
            if (pin is null)
            {
                PinLockToggle.IsOn = false;
                return;
            }
            _lockService.SetPin(pin);
        }
        else
        {
            _lockService.DisablePinLock();
            WindowsHelloToggle.IsOn = false;
        }

        UpdateSecurityCardVisibility();
    }

    private async void WindowsHelloToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;

        if (WindowsHelloToggle.IsOn)
        {
            if (!await _lockService.IsWindowsHelloAvailableAsync())
            {
                WindowsHelloToggle.IsOn = false;
                await ShowMessageAsync(
                    "Windows Hello unavailable",
                    "Windows Hello isn't set up on this device. Set it up in Windows Settings → Accounts → Sign-in options, then try again.");
                return;
            }
        }

        _lockService.SetWindowsHelloEnabled(WindowsHelloToggle.IsOn);
    }

    private async void ChangePinCard_Click(object sender, RoutedEventArgs e)
    {
        var pin = await ShowSetPinDialogAsync("Change PIN", "Choose a new 4–6 digit PIN.");
        if (pin is not null)
            _lockService.SetPin(pin);
    }

    private async Task<string?> ShowSetPinDialogAsync(string title, string description)
    {
        var descriptionText = new TextBlock { Text = description, Opacity = 0.7, TextWrapping = TextWrapping.Wrap };
        var pinBox = new PasswordBox { Header = "New PIN", MaxLength = 6, PasswordRevealMode = PasswordRevealMode.Peek };
        var confirmBox = new PasswordBox { Header = "Confirm PIN", MaxLength = 6, PasswordRevealMode = PasswordRevealMode.Peek };
        var errorText = new TextBlock { Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red), TextWrapping = TextWrapping.Wrap, Visibility = Visibility.Collapsed };

        var panel = new StackPanel { Spacing = 12, MinWidth = 300 };
        panel.Children.Add(descriptionText);
        panel.Children.Add(pinBox);
        panel.Children.Add(confirmBox);
        panel.Children.Add(errorText);

        var dialog = new ContentDialog
        {
            Title = title,
            PrimaryButtonText = "Save",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            Content = panel,
            XamlRoot = XamlRoot
        };

        dialog.PrimaryButtonClick += (_, args) =>
        {
            var pin = pinBox.Password;
            if (pin.Length < 4 || pin.Length > 6 || !pin.All(char.IsDigit))
            {
                errorText.Text = "PIN must be 4–6 digits.";
                errorText.Visibility = Visibility.Visible;
                args.Cancel = true;
                return;
            }
            if (pin != confirmBox.Password)
            {
                errorText.Text = "PINs don't match.";
                errorText.Visibility = Visibility.Visible;
                args.Cancel = true;
            }
        };

        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary ? pinBox.Password : null;
    }

    private async void GoogleDriveConnect_Click(object sender, RoutedEventArgs e)
    {
        if (_googleDrive.IsSignedIn)
        {
            _googleDrive.SignOut();
            var settings = App.Host.Services.GetRequiredService<SettingsService>();
            settings.Current.IsGoogleDriveAutoBackupEnabled = false;
            settings.Save();
            GoogleDriveAutoBackupToggle.IsOn = false;
            UpdateGoogleDriveDisconnectedUi();
            return;
        }

        GoogleDriveConnectButton.IsEnabled = false;
        GoogleDriveStatusText.Text = "Waiting for sign-in in your browser…";
        try
        {
            var success = await _googleDrive.SignInAsync();
            if (success)
                await UpdateGoogleDriveConnectedUiAsync();
            else
                UpdateGoogleDriveDisconnectedUi();
        }
        finally
        {
            GoogleDriveConnectButton.IsEnabled = true;
        }
    }

    private async void GoogleDriveAutoBackupToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;
        var settings = App.Host.Services.GetRequiredService<SettingsService>();
        settings.Current.IsGoogleDriveAutoBackupEnabled = GoogleDriveAutoBackupToggle.IsOn;
        settings.Save();

        // Auto backup otherwise only runs at the next app launch — turning it on should back up
        // right away rather than leaving the user wondering why nothing happened until a restart.
        if (GoogleDriveAutoBackupToggle.IsOn)
            await BackupToGoogleDriveAsync(showSuccessMessage: false);
    }

    private async void GoogleDriveBackupNow_Click(object sender, RoutedEventArgs e) =>
        await BackupToGoogleDriveAsync(showSuccessMessage: true);

    private async Task BackupToGoogleDriveAsync(bool showSuccessMessage)
    {
        try
        {
            var tempPath = Path.Combine(Path.GetTempPath(), $"ExpenseManagerPro-Backup-{Guid.NewGuid():N}.embackup");
            await _backupService.BackupToAsync(tempPath);
            try
            {
                await _googleDrive.UploadBackupAsync(tempPath);
            }
            finally
            {
                File.Delete(tempPath);
            }

            var settings = App.Host.Services.GetRequiredService<SettingsService>();
            settings.Current.LastGoogleDriveBackupUtc = DateTime.UtcNow;
            settings.Save();

            if (showSuccessMessage)
                await ShowMessageAsync("Backup complete", "Your data was backed up to Google Drive.");
        }
        catch (Exception ex)
        {
            await ShowMessageAsync("Backup failed", ex.Message);
        }
    }

    private async void GoogleDriveRestore_Click(object sender, RoutedEventArgs e)
    {
        List<GoogleDriveBackupInfo> backups;
        try
        {
            backups = await _googleDrive.ListBackupsAsync();
        }
        catch (Exception ex)
        {
            await ShowMessageAsync("Couldn't list backups", ex.Message);
            return;
        }

        if (backups.Count == 0)
        {
            await ShowMessageAsync("No backups found", "There are no backups in Google Drive for this account yet.");
            return;
        }

        var listView = new ListView { SelectionMode = ListViewSelectionMode.Single, MaxHeight = 320 };
        foreach (var backup in backups)
        {
            var when = backup.CreatedUtc?.ToLocalTime().ToString("MMM d, yyyy h:mm tt") ?? "Unknown date";
            listView.Items.Add(new ListViewItem { Content = $"{when}", Tag = backup });
        }
        listView.SelectedIndex = 0;

        var dialog = new ContentDialog
        {
            Title = "Restore from Google Drive",
            Content = new StackPanel
            {
                Spacing = 8,
                Children =
                {
                    new TextBlock
                    {
                        Text = "Choose a backup. This replaces all current data and restarts the app.",
                        Opacity = 0.7,
                        TextWrapping = TextWrapping.Wrap
                    },
                    listView
                }
            },
            PrimaryButtonText = "Restore",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot
        };

        if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;
        if (listView.SelectedItem is not ListViewItem { Tag: GoogleDriveBackupInfo selected }) return;

        try
        {
            var tempPath = Path.Combine(Path.GetTempPath(), $"ExpenseManagerPro-Restore-{Guid.NewGuid():N}.embackup");
            await _googleDrive.DownloadBackupAsync(selected.FileId, tempPath);
            _backupService.RestoreFrom(tempPath);
            File.Delete(tempPath);

            var exePath = Process.GetCurrentProcess().MainModule?.FileName;
            if (exePath is not null)
                Process.Start(exePath);
            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            await ShowMessageAsync("Restore failed", ex.Message);
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
