using System.Collections.Concurrent;
using PaycheckCalculator.TaxRules.Registry;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<IRulePackageRegistry, InMemoryRulePackageRegistry>();
builder.Services.AddSingleton<ConcurrentDictionary<Guid, EncryptedEnvelopeRecord>>();
builder.Services.AddSingleton<ConcurrentDictionary<Guid, TrustedDeviceRecord>>();
builder.Services.AddSingleton<ConcurrentDictionary<Guid, byte[]>>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Tax rule packages: download signed JSON for an installed tax year.
app.MapGet("/v1/rules/{taxYear:int}", (int taxYear, IRulePackageRegistry registry) =>
{
    var bundle = registry.GetBundle(new PaycheckCalculator.Core.ValueObjects.TaxYear(taxYear));
    return Results.Ok(new
    {
        bundle.TaxYear.Year,
        federal = bundle.Federal,
        states = bundle.States,
        locals = bundle.Locals
    });
}).WithName("GetRuleBundle");

// Encrypted sync envelopes - server NEVER reads ciphertext content.
app.MapPost("/v1/sync/envelopes",
    (EncryptedEnvelopeRecord envelope, ConcurrentDictionary<Guid, EncryptedEnvelopeRecord> store) =>
    {
        store[envelope.SyncItemId] = envelope;
        return Results.Created($"/v1/sync/envelopes/{envelope.SyncItemId}", envelope);
    });

app.MapGet("/v1/sync/envelopes",
    (long? sinceVersion, ConcurrentDictionary<Guid, EncryptedEnvelopeRecord> store) =>
    {
        var items = store.Values
            .Where(e => sinceVersion is null || e.ItemVersion > sinceVersion)
            .OrderBy(e => e.ItemVersion)
            .ToArray();
        return Results.Ok(items);
    });

app.MapPut("/v1/sync/envelopes/{syncItemId:guid}",
    (Guid syncItemId, EncryptedEnvelopeRecord envelope, ConcurrentDictionary<Guid, EncryptedEnvelopeRecord> store) =>
    {
        if (envelope.SyncItemId != syncItemId)
            return Results.BadRequest("syncItemId mismatch");
        store[syncItemId] = envelope;
        return Results.NoContent();
    });

// Devices
app.MapPost("/v1/devices",
    (TrustedDeviceRecord device, ConcurrentDictionary<Guid, TrustedDeviceRecord> store) =>
    {
        store[device.DeviceId] = device;
        return Results.Created($"/v1/devices/{device.DeviceId}", device);
    });

app.MapPost("/v1/devices/{deviceId:guid}/approve",
    (Guid deviceId, ConcurrentDictionary<Guid, TrustedDeviceRecord> store) =>
        store.TryGetValue(deviceId, out var d)
            ? Results.Ok(d with { Approved = true })
            : Results.NotFound());

app.MapPost("/v1/devices/{deviceId:guid}/revoke",
    (Guid deviceId, ConcurrentDictionary<Guid, TrustedDeviceRecord> store) =>
    {
        if (store.TryGetValue(deviceId, out var d))
        {
            store[deviceId] = d with { Revoked = true };
            return Results.NoContent();
        }
        return Results.NotFound();
    });

// Wrapped vault keys
app.MapPost("/v1/vault/wrapped-keys",
    (WrappedKeyRecord record, ConcurrentDictionary<Guid, byte[]> store) =>
    {
        store[record.DeviceId] = record.WrappedKey;
        return Results.NoContent();
    });

app.MapGet("/v1/vault/wrapped-keys/{deviceId:guid}",
    (Guid deviceId, ConcurrentDictionary<Guid, byte[]> store) =>
        store.TryGetValue(deviceId, out var key)
            ? Results.Ok(new { DeviceId = deviceId, WrappedKey = key })
            : Results.NotFound());

app.Run();

public partial class Program;

public sealed record EncryptedEnvelopeRecord(
    Guid SyncItemId, Guid UserId, Guid OwnerDeviceId, string ItemKind, long ItemVersion,
    DateTimeOffset UpdatedAtUtc, byte[] Nonce, byte[] Ciphertext, byte[] CiphertextHash, byte[] Signature);

public sealed record TrustedDeviceRecord(
    Guid DeviceId, Guid UserId, string? DeviceName, byte[] PublicDeviceKey,
    DateTimeOffset CreatedAtUtc, bool Approved = false, bool Revoked = false);

public sealed record WrappedKeyRecord(Guid UserId, Guid DeviceId, byte[] WrappedKey);
