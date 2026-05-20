using Microsoft.Maui.Storage;

namespace PaycheckCalculator.Maui.Services;

/// <summary>
/// Maps the abstract secure-storage interface onto MAUI <see cref="SecureStorage"/>, which is backed by
/// platform Keychain (iOS/macOS), KeyStore (Android), and DPAPI (Windows). Values are stored as base64
/// strings so binary keys round-trip through the underlying string-only API.
/// </summary>
public sealed class MauiSecureStorage : ISecurePlatformStorage
{
    public async Task<byte[]?> GetAsync(string key, CancellationToken ct = default)
    {
        var encoded = await SecureStorage.Default.GetAsync(key).ConfigureAwait(false);
        return encoded is null ? null : Convert.FromBase64String(encoded);
    }

    public Task SetAsync(string key, byte[] value, CancellationToken ct = default) =>
        SecureStorage.Default.SetAsync(key, Convert.ToBase64String(value));

    public Task RemoveAsync(string key, CancellationToken ct = default)
    {
        SecureStorage.Default.Remove(key);
        return Task.CompletedTask;
    }

    public Task ClearAllAsync(CancellationToken ct = default)
    {
        SecureStorage.Default.RemoveAll();
        return Task.CompletedTask;
    }
}
