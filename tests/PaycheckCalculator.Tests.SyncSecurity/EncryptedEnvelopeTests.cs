using System.Security.Cryptography;
using FluentAssertions;
using PaycheckCalculator.Sync;
using Xunit;

namespace PaycheckCalculator.Tests.SyncSecurity;

public class EncryptedEnvelopeTests
{
    private readonly EnvelopeEncryptor _enc = new();

    [Fact]
    public void Sensitive_amounts_never_appear_in_ciphertext_bytes()
    {
        var key = RandomNumberGenerator.GetBytes(32);
        var payload = new SensitivePayload(
            EmployerName: "Acme Inc",
            GrossPay: 123_456.78m,
            FederalWithholding: 9_876.54m);

        var envelope = _enc.Encrypt(Guid.NewGuid(), "paycheck", 1, payload, key);

        var asString = System.Text.Encoding.UTF8.GetString(envelope.Ciphertext);
        asString.Should().NotContain("Acme");
        asString.Should().NotContain("123456");
        asString.Should().NotContain("9876");
    }

    [Fact]
    public void Decrypt_round_trips_payload()
    {
        var key = RandomNumberGenerator.GetBytes(32);
        var payload = new SensitivePayload("Acme Inc", 100m, 10m);
        var envelope = _enc.Encrypt(Guid.NewGuid(), "paycheck", 1, payload, key);

        var decrypted = _enc.Decrypt<SensitivePayload>(envelope, key);

        decrypted.Should().Be(payload);
    }

    [Fact]
    public void Decrypt_with_wrong_key_throws()
    {
        var key = RandomNumberGenerator.GetBytes(32);
        var wrongKey = RandomNumberGenerator.GetBytes(32);
        var envelope = _enc.Encrypt(Guid.NewGuid(), "paycheck", 1, new SensitivePayload("x", 1m, 1m), key);

        var act = () => _enc.Decrypt<SensitivePayload>(envelope, wrongKey);

        act.Should().Throw<CryptographicException>();
    }

    public sealed record SensitivePayload(string EmployerName, decimal GrossPay, decimal FederalWithholding);
}
