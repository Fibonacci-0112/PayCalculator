namespace PaycheckCalculator.Maui.Services;

/// <summary>
/// MAUI does not ship a first-party biometric API; production should reference a vetted plugin (e.g.
/// Plugin.Fingerprint or Plugin.Maui.Biometric). This stub implements the interface so DI resolves and
/// the UI shows the passphrase-only path until the plugin is wired in.
/// </summary>
public sealed class MauiBiometricUnlock : IBiometricUnlock
{
    public bool IsAvailable => false;

    public Task<bool> AuthenticateAsync(string reason, CancellationToken ct = default) =>
        Task.FromResult(false);
}
