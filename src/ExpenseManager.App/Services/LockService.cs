using System.Security.Cryptography;
using System.Text;
using Windows.Security.Credentials.UI;

namespace ExpenseManager.App.Services;

public class LockService(SettingsService settings)
{
    public bool IsPinLockEnabled => settings.Current.IsPinLockEnabled;
    public bool IsWindowsHelloEnabled => settings.Current.IsWindowsHelloEnabled;

    public void SetPin(string pin)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        settings.Current.PinSalt = Convert.ToBase64String(salt);
        settings.Current.PinHash = Hash(pin, salt);
        settings.Current.IsPinLockEnabled = true;
        settings.Save();
    }

    public void DisablePinLock()
    {
        settings.Current.IsPinLockEnabled = false;
        settings.Current.IsWindowsHelloEnabled = false;
        settings.Current.PinHash = null;
        settings.Current.PinSalt = null;
        settings.Save();
    }

    public bool VerifyPin(string pin)
    {
        if (settings.Current.PinHash is null || settings.Current.PinSalt is null) return false;
        var salt = Convert.FromBase64String(settings.Current.PinSalt);
        return Hash(pin, salt) == settings.Current.PinHash;
    }

    public void SetWindowsHelloEnabled(bool enabled)
    {
        settings.Current.IsWindowsHelloEnabled = enabled;
        settings.Save();
    }

    public async Task<bool> IsWindowsHelloAvailableAsync()
    {
        try
        {
            var availability = await UserConsentVerifier.CheckAvailabilityAsync();
            return availability == UserConsentVerifierAvailability.Available;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Attempts a Windows Hello (biometric/PIN) prompt. Returns false — never throws —
    /// if Windows Hello isn't available or the attempt fails, so callers can fall back to the
    /// app's own PIN entry without special-casing errors.</summary>
    public async Task<bool> TryVerifyWithWindowsHelloAsync()
    {
        try
        {
            var result = await UserConsentVerifier.RequestVerificationAsync("Unlock Expense Manager Pro");
            return result == UserConsentVerificationResult.Verified;
        }
        catch
        {
            return false;
        }
    }

    private static string Hash(string pin, byte[] salt)
    {
        var bytes = Encoding.UTF8.GetBytes(pin);
        var combined = new byte[bytes.Length + salt.Length];
        bytes.CopyTo(combined, 0);
        salt.CopyTo(combined, bytes.Length);
        return Convert.ToBase64String(SHA256.HashData(combined));
    }
}
