namespace PaycheckCalculator.Maui.Services;

/// <summary>
/// Resolves on-device storage paths from <see cref="FileSystem.AppDataDirectory"/> (per-user, sandboxed
/// on every supported platform) and <see cref="FileSystem.CacheDirectory"/> (OS-evictable).
/// </summary>
public sealed class MauiLocalDataPathProvider : ILocalDataPathProvider
{
    public MauiLocalDataPathProvider()
    {
        EncryptedDatabasePath = Path.Combine(FileSystem.AppDataDirectory, "paycheck-planner.db");
        LocalCacheFolderPath = Path.Combine(FileSystem.CacheDirectory, "paycheck-planner");
        Directory.CreateDirectory(LocalCacheFolderPath);
    }

    public string EncryptedDatabasePath { get; }
    public string LocalCacheFolderPath { get; }
}
