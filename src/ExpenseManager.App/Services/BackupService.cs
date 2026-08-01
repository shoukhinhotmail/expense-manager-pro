using Microsoft.Data.Sqlite;

namespace ExpenseManager.App.Services;

public class BackupService
{
    public string DbPath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ExpenseManagerPro", "expensemanager.db");

    /// <summary>Creates a consistent copy of the live database using SQLite's online backup API
    /// (safe to call while the app is running, correctly handles WAL mode).</summary>
    public async Task BackupToAsync(string destinationFilePath, CancellationToken ct = default)
    {
        var destinationConnectionString = $"Data Source={destinationFilePath}";
        try
        {
            await using (var source = new SqliteConnection($"Data Source={DbPath};Mode=ReadOnly"))
            await using (var destination = new SqliteConnection(destinationConnectionString))
            {
                await source.OpenAsync(ct);
                await destination.OpenAsync(ct);
                source.BackupDatabase(destination);
            }
        }
        finally
        {
            // Microsoft.Data.Sqlite pools connections by default, which can keep the destination
            // file's OS-level handle open for a moment after the connection is disposed. Callers
            // that immediately read or delete this file (e.g. uploading it, then cleaning it up)
            // need the handle released for real before this method returns.
            SqliteConnection.ClearPool(new SqliteConnection(destinationConnectionString));
        }
    }

    /// <summary>Copies a backup file over the live database. The app must restart afterwards
    /// so it opens a fresh connection to the restored file.</summary>
    public void RestoreFrom(string sourceFilePath)
    {
        // The app's own EF Core connection to the live database is still pooled at this point
        // (Microsoft.Data.Sqlite keeps native handles open even after a SqliteConnection is
        // disposed), which locks the -wal/-shm files we're about to overwrite/delete. Clearing
        // every pooled connection app-wide releases them before we touch the files.
        SqliteConnection.ClearAllPools();

        File.Copy(sourceFilePath, DbPath, overwrite: true);

        var walPath = DbPath + "-wal";
        var shmPath = DbPath + "-shm";
        if (File.Exists(walPath)) File.Delete(walPath);
        if (File.Exists(shmPath)) File.Delete(shmPath);
    }
}
