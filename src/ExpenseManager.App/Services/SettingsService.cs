using System.Text.Json;

namespace ExpenseManager.App.Services;

public class AppSettings
{
    public string Theme { get; set; } = "Default"; // Default (System), Light, Dark
    public string? CurrencyCode { get; set; }

    public bool IsPinLockEnabled { get; set; }
    public string? PinHash { get; set; }
    public string? PinSalt { get; set; }
    public bool IsWindowsHelloEnabled { get; set; }

    public List<DashboardWidgetConfig>? DashboardLayout { get; set; }

    public bool IsGoogleDriveAutoBackupEnabled { get; set; }
    public DateTime? LastGoogleDriveBackupUtc { get; set; }
}

public class SettingsService
{
    private readonly string _filePath;
    private AppSettings _settings;

    public SettingsService()
    {
        var dataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ExpenseManagerPro");
        Directory.CreateDirectory(dataDir);
        _filePath = Path.Combine(dataDir, "settings.json");
        _settings = Load();
    }

    public AppSettings Current => _settings;

    public void Save()
    {
        var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_filePath, json);
    }

    private AppSettings Load()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);
                if (settings is not null) return settings;
            }
        }
        catch (JsonException)
        {
            // Corrupt settings file: fall back to defaults.
        }

        return new AppSettings();
    }
}
