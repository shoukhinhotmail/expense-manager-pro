using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Requests;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;

namespace ExpenseManager.App.Services;

public record GoogleDriveBackupInfo(string FileId, string Name, DateTime? CreatedUtc, long? SizeBytes);

/// <summary>Google only issues a refresh_token on the very first consent grant for a given
/// user+app unless the request explicitly forces the consent screen every time. Without one, the
/// cached session works until the short-lived access token expires (~1 hour) and then silently
/// stops — exactly the "auto backup doesn't run after reopening the app" symptom. Forcing
/// prompt=consent guarantees a fresh refresh_token on every sign-in.</summary>
internal class ConsentForcingCodeFlow(GoogleAuthorizationCodeFlow.Initializer initializer) : GoogleAuthorizationCodeFlow(initializer)
{
    public override AuthorizationCodeRequestUrl CreateAuthorizationCodeRequest(string redirectUri)
    {
        var url = (GoogleAuthorizationCodeRequestUrl)base.CreateAuthorizationCodeRequest(redirectUri);
        url.AccessType = "offline";
        url.Prompt = "consent";
        return url;
    }
}

public class GoogleDriveBackupService
{
    // Desktop-app OAuth client. Google doesn't treat this as a fully confidential secret for
    // installed apps (they can't keep one), which is exactly why the "Desktop app" client type
    // exists — security relies on the limited drive.file scope and per-user consent, not on this
    // value being hidden.
    private const string ClientId = "207384795296-80fk3gbgtk2m37aup6mb9eu3tfv9d22n.apps.googleusercontent.com";
    private const string ClientSecret = "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";
    private const string AppFolderName = "Expense Manager Pro Backups";

    private static readonly string[] Scopes = [DriveService.Scope.DriveFile];
    private static readonly string TokenStorePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ExpenseManagerPro", "GoogleTokens");

    private DriveService? _driveService;
    private string? _folderId;

    public bool IsSignedIn => _driveService is not null;

    public async Task<bool> SignInAsync(CancellationToken ct = default)
    {
        try
        {
            var flow = new ConsentForcingCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets { ClientId = ClientId, ClientSecret = ClientSecret },
                Scopes = Scopes,
                DataStore = new FileDataStore(TokenStorePath, true)
            });

            var app = new AuthorizationCodeInstalledApp(flow, new LocalServerCodeReceiver());
            var credential = await app.AuthorizeAsync("user", ct);

            _driveService = new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "Expense Manager Pro"
            });
            return true;
        }
        catch
        {
            _driveService = null;
            return false;
        }
    }

    public void SignOut()
    {
        _driveService?.Dispose();
        _driveService = null;
        _folderId = null;
        if (Directory.Exists(TokenStorePath))
            Directory.Delete(TokenStorePath, recursive: true);
    }

    /// <summary>Attempts to reuse a previously cached sign-in without showing any UI. Returns
    /// false (not true) if the user has never signed in or the cached token is gone/revoked.</summary>
    public async Task<bool> TryRestoreSessionAsync(CancellationToken ct = default)
    {
        if (!Directory.Exists(TokenStorePath)) return false;
        return await SignInAsync(ct);
    }

    public async Task<string?> GetSignedInEmailAsync(CancellationToken ct = default)
    {
        if (_driveService is null) return null;
        try
        {
            var about = _driveService.About.Get();
            about.Fields = "user";
            var result = await about.ExecuteAsync(ct);
            return result.User?.EmailAddress;
        }
        catch
        {
            return null;
        }
    }

    private async Task<string> GetOrCreateAppFolderAsync(CancellationToken ct)
    {
        if (_folderId is not null) return _folderId;
        if (_driveService is null) throw new InvalidOperationException("Not signed in.");

        var list = _driveService.Files.List();
        list.Q = $"name = '{AppFolderName}' and mimeType = 'application/vnd.google-apps.folder' and trashed = false";
        list.Fields = "files(id, name)";
        var result = await list.ExecuteAsync(ct);

        var existing = result.Files?.FirstOrDefault();
        if (existing is not null)
        {
            _folderId = existing.Id;
            return _folderId;
        }

        var folder = new Google.Apis.Drive.v3.Data.File
        {
            Name = AppFolderName,
            MimeType = "application/vnd.google-apps.folder"
        };
        var created = await _driveService.Files.Create(folder).ExecuteAsync(ct);
        _folderId = created.Id;
        return _folderId;
    }

    public async Task UploadBackupAsync(string localFilePath, CancellationToken ct = default)
    {
        if (_driveService is null) throw new InvalidOperationException("Not signed in.");

        var folderId = await GetOrCreateAppFolderAsync(ct);
        var fileName = $"ExpenseManagerPro-Backup-{DateTime.Now:yyyy-MM-dd-HHmmss}.embackup";

        var metadata = new Google.Apis.Drive.v3.Data.File
        {
            Name = fileName,
            Parents = [folderId]
        };

        await using var stream = File.OpenRead(localFilePath);
        var request = _driveService.Files.Create(metadata, stream, "application/octet-stream");
        await request.UploadAsync(ct);

        // Keep only the single most recent backup — no reason to eat into the user's Drive
        // storage with a growing history when each backup is a full snapshot anyway.
        await PruneOldBackupsAsync(folderId, keep: 1, ct);
    }

    private async Task PruneOldBackupsAsync(string folderId, int keep, CancellationToken ct)
    {
        if (_driveService is null) return;

        var list = _driveService.Files.List();
        list.Q = $"'{folderId}' in parents and trashed = false";
        list.Fields = "files(id, name, createdTime)";
        list.OrderBy = "createdTime desc";
        var result = await list.ExecuteAsync(ct);

        var files = result.Files ?? [];
        foreach (var old in files.Skip(keep))
        {
            try { await _driveService.Files.Delete(old.Id).ExecuteAsync(ct); }
            catch { /* best-effort cleanup */ }
        }
    }

    public async Task<List<GoogleDriveBackupInfo>> ListBackupsAsync(CancellationToken ct = default)
    {
        if (_driveService is null) throw new InvalidOperationException("Not signed in.");

        var folderId = await GetOrCreateAppFolderAsync(ct);
        var list = _driveService.Files.List();
        list.Q = $"'{folderId}' in parents and trashed = false";
        list.Fields = "files(id, name, createdTime, size)";
        list.OrderBy = "createdTime desc";
        var result = await list.ExecuteAsync(ct);

        return (result.Files ?? [])
            .Select(f => new GoogleDriveBackupInfo(f.Id, f.Name, f.CreatedTimeDateTimeOffset?.UtcDateTime, f.Size))
            .ToList();
    }

    public async Task DownloadBackupAsync(string fileId, string destinationPath, CancellationToken ct = default)
    {
        if (_driveService is null) throw new InvalidOperationException("Not signed in.");

        await using var output = File.Create(destinationPath);
        await _driveService.Files.Get(fileId).DownloadAsync(output, ct);
    }
}
