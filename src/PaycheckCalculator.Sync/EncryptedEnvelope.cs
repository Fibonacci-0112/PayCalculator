using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PaycheckCalculator.Sync;

public sealed record EncryptedEnvelope(
    Guid SyncItemId,
    string ItemKind,
    long ItemVersion,
    DateTimeOffset UpdatedAt,
    byte[] Nonce,
    byte[] Ciphertext,
    byte[] CiphertextHash);

public sealed class EnvelopeEncryptor
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    public EncryptedEnvelope Encrypt<T>(Guid syncItemId, string itemKind, long version, T payload, byte[] dataKey)
    {
        if (dataKey.Length != 32) throw new ArgumentException("AES-GCM data key must be 32 bytes.", nameof(dataKey));
        var json = JsonSerializer.SerializeToUtf8Bytes(payload, JsonOptions);
        var nonce = RandomNumberGenerator.GetBytes(12);
        var ciphertext = new byte[json.Length];
        var tag = new byte[16];
        using (var aes = new AesGcm(dataKey, tag.Length))
        {
            aes.Encrypt(nonce, json, ciphertext, tag);
        }
        var combined = new byte[ciphertext.Length + tag.Length];
        Buffer.BlockCopy(ciphertext, 0, combined, 0, ciphertext.Length);
        Buffer.BlockCopy(tag, 0, combined, ciphertext.Length, tag.Length);
        var hash = SHA256.HashData(combined);
        return new EncryptedEnvelope(syncItemId, itemKind, version, DateTimeOffset.UtcNow, nonce, combined, hash);
    }

    public T Decrypt<T>(EncryptedEnvelope envelope, byte[] dataKey)
    {
        var ciphertext = envelope.Ciphertext.AsSpan(0, envelope.Ciphertext.Length - 16);
        var tag = envelope.Ciphertext.AsSpan(envelope.Ciphertext.Length - 16);
        var plaintext = new byte[ciphertext.Length];
        using (var aes = new AesGcm(dataKey, tag.Length))
        {
            aes.Decrypt(envelope.Nonce, ciphertext, tag, plaintext);
        }
        var payload = JsonSerializer.Deserialize<T>(plaintext, JsonOptions)
            ?? throw new InvalidOperationException("Decrypted payload was null.");
        return payload;
    }

    public string AsBase64(byte[] bytes) => Convert.ToBase64String(bytes);

    public string PayloadAsString(EncryptedEnvelope envelope) => Encoding.UTF8.GetString(envelope.Ciphertext);
}
