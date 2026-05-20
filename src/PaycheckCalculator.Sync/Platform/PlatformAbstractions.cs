namespace PaycheckCalculator.Sync.Platform;

/// <summary>
/// Per-device secure storage for the vault key, the wrapped device key pair, and the recovery-key derived
/// secret. MAUI maps this to platform secure storage (Keychain / KeyStore / DPAPI / macOS Keychain).
/// The Web client maps it to a session-scoped IndexedDB record decrypted with a key derived from the
/// account passphrase.
/// </summary>
public interface ISecurePlatformStorage
{
    Task<byte[]?> GetAsync(string key, CancellationToken ct = default);
    Task SetAsync(string key, byte[] value, CancellationToken ct = default);
    Task RemoveAsync(string key, CancellationToken ct = default);
    Task ClearAllAsync(CancellationToken ct = default);
}

/// <summary>
/// Resolves the on-disk path where the encrypted SQLite database (MAUI) or encrypted IndexedDB shim
/// (Web) lives. Tests pass an in-memory provider so calculation code never touches a real filesystem.
/// </summary>
public interface ILocalDataPathProvider
{
    /// <summary>Absolute path to the encrypted SQLite database file for MAUI; an opaque identifier on Web.</summary>
    string EncryptedDatabasePath { get; }

    /// <summary>Folder where exports / backups / signed rule packages are cached. Local-first, never uploaded.</summary>
    string LocalCacheFolderPath { get; }
}

/// <summary>
/// Biometric unlock (Face ID / Touch ID / Windows Hello / Android BiometricPrompt). When unsupported on a
/// platform the implementation must report <see cref="IsAvailable"/> = false so the UI can fall back to a
/// passphrase prompt instead of silently degrading security.
/// </summary>
public interface IBiometricUnlock
{
    bool IsAvailable { get; }

    /// <summary>Prompts the user to authenticate. Returns true on success, false on cancel or failure.</summary>
    Task<bool> AuthenticateAsync(string reason, CancellationToken ct = default);
}

/// <summary>
/// App lifecycle hooks the SharedUi layer subscribes to so it can lock the vault, persist YTD deltas, and
/// flush diagnostics when the OS sends a backgrounded / resumed / sleep signal.
/// </summary>
public interface IAppLifecycleEvents
{
    event Action? Resumed;
    event Action? Backgrounded;
    event Action? Sleeping;
}
