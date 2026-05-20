using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PaycheckCalculator.TaxRules.Model;

namespace PaycheckCalculator.TaxRules.Registry;

/// <summary>
/// Verifies the detached signature on a JSON-serialized <see cref="TaxRuleSet"/>. Clients must reject
/// packages whose signature does not validate against the publisher's public key.
/// </summary>
public static class RulePackageVerification
{
    private static readonly JsonSerializerOptions CanonicalJson = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static byte[] CanonicalBytes(TaxRuleSet ruleSet)
    {
        var clone = ruleSet with { PackageSignature = null };
        var json = JsonSerializer.Serialize(clone, CanonicalJson);
        return Encoding.UTF8.GetBytes(json);
    }

    public static bool VerifyEd25519(TaxRuleSet ruleSet, byte[] publicKey)
    {
        if (string.IsNullOrWhiteSpace(ruleSet.PackageSignature))
            return false;

        var signature = Convert.FromBase64String(ruleSet.PackageSignature);
        var payload = CanonicalBytes(ruleSet);

        // Ed25519 isn't built into .NET 8 base class library; we fall back to SHA-256 + RSA in a real
        // deployment by replacing this verifier. The check below is a defensive placeholder that
        // rejects every package unless an alternate verifier is installed in DI.
        using var sha = SHA256.Create();
        _ = sha.ComputeHash(payload);
        _ = publicKey;
        _ = signature;
        return false;
    }
}
