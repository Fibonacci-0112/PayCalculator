using System.Security.Cryptography;
using System.Text.Json;
using PaycheckCalculator.TaxRules.Model;

if (args.Length < 2)
{
    Console.Error.WriteLine("Usage: rule-package-signer <input.json> <output.json> [<rsa-private-key.pem>]");
    return 1;
}

var inputPath = args[0];
var outputPath = args[1];
var keyPath = args.Length >= 3 ? args[2] : null;

var ruleSet = JsonSerializer.Deserialize<TaxRuleSet>(
    File.ReadAllText(inputPath),
    new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
    ?? throw new InvalidOperationException("Failed to parse rule set.");

var unsigned = ruleSet with { PackageSignature = null };
var canonical = JsonSerializer.SerializeToUtf8Bytes(unsigned, new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
});

string signatureB64;
using var rsa = RSA.Create(3072);
if (keyPath is not null)
{
    rsa.ImportFromPem(File.ReadAllText(keyPath));
}
var signature = rsa.SignData(canonical, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
signatureB64 = Convert.ToBase64String(signature);

var signed = unsigned with { PackageSignature = signatureB64 };
File.WriteAllText(outputPath, JsonSerializer.Serialize(signed, new JsonSerializerOptions
{
    WriteIndented = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
}));
Console.WriteLine($"Signed rule set {ruleSet.RuleSetId} -> {outputPath}");
return 0;
