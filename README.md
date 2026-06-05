# PayCalculator — Paycheck Tax Planning App

Cross-platform paycheck, tax-planning, and payday-budgeting application built per **SPEC-1-Paycheck-Tax-Planning-App**.

The first tax-rule target is **tax year 2026**. The solution is a multi-project .NET solution using the new `.slnx` (XML) format. Calculation logic lives in shared .NET class libraries; Razor components live in a shared UI library consumed by both a Blazor Web App and a .NET MAUI Blazor Hybrid client.

> Estimates only. The app provides planning assistance, not professional tax advice.

## Solution layout

```
PaycheckCalculator.slnx          # New XML solution format
Directory.Build.props            # Strict nullable, warnings-as-errors, deterministic builds
Directory.Packages.props         # Central package management
global.json                      # SDK pin

/src
  PaycheckCalculator.Core            # Value objects, contracts, explainability, audit
  PaycheckCalculator.Payroll         # Earnings normalizer, deduction classifier, FIT, FICA, supplemental
  PaycheckCalculator.TaxRules        # Versioned, signed rule packages + 2026 federal data
  PaycheckCalculator.Jurisdictions   # Address/jurisdiction resolver + state/local engine
  PaycheckCalculator.Projections     # Annual 1040-style projection + optimization engine
  PaycheckCalculator.SelfEmployment  # Schedule C/SE/QBI estimator
  PaycheckCalculator.Budgeting       # Payday-based budgets, categories, allocator
  PaycheckCalculator.Reports         # Immutable report DTOs
  PaycheckCalculator.ImportExport    # CSV import/export
  PaycheckCalculator.Sync            # E2EE envelope encrypt/decrypt (AES-GCM)
  PaycheckCalculator.SharedUi        # Razor class library (components used by Web + MAUI)
  PaycheckCalculator.Maui            # .NET MAUI Blazor Hybrid client (Android, iOS, Windows, macOS)
  PaycheckCalculator.Web             # Blazor Web App host
  PaycheckCalculator.Api             # ASP.NET Core minimal API: sync, rule download, devices
  PaycheckCalculator.RuleAdmin       # Rule authoring/import/review/sign/publish workflow

/tests
  PaycheckCalculator.Tests.Golden        # Golden case runner (JSON fixtures)
  PaycheckCalculator.Tests.TaxRules      # Rule package & registry tests
  PaycheckCalculator.Tests.Jurisdictions # Resolver tests
  PaycheckCalculator.Tests.Payroll       # Federal withholding, FICA, supplemental, YTD, warnings, audit
  PaycheckCalculator.Tests.Projections   # Optimization engine
  PaycheckCalculator.Tests.SyncSecurity  # AES-GCM envelope leakage tests

/tools
  TaxRuleImporter           # Emit canonical rule JSON
  RulePackageSigner         # Sign rule JSON with RSA-3072 / SHA-256
  JurisdictionDataImporter  # Stub for boundary/PSD import
  GoldenCaseRunner          # CLI runner for golden fixtures

/rules
  2026/federal/federal-2026-pub15t.json  # Canonical 2026 federal rule data
```

The MAUI client (`src/PaycheckCalculator.Maui/`) is deliberately *not* in `PaycheckCalculator.slnx` so the rest of the solution restores and tests on a vanilla .NET SDK install (CI on `ubuntu-latest` does not have the MAUI workload). To build the MAUI head locally:

```bash
dotnet workload install maui
dotnet build src/PaycheckCalculator.Maui/PaycheckCalculator.Maui.csproj -f net11.0-android
# or -f net11.0-ios / net11.0-maccatalyst / net11.0-windows10.0.19041.0 on the appropriate host OS
```

Project references inside the MAUI csproj pull in `PaycheckCalculator.SharedUi`, `PaycheckCalculator.Sync`, and `PaycheckCalculator.ImportExport` transitively, so a single build command compiles everything the MAUI head needs.

What the MAUI head provides on top of `SharedUi`:

- `MauiProgram.cs` wires `AddPaycheckCalculatorCore()` (the same DI extension the Blazor Web App uses), so calculation, optimization, projection, scenario store, jurisdiction resolver, and rule registry come from the shared library.
- `BlazorWebView` in `MainPage.xaml` hosts the shared `Routes` component, which mounts every page under `src/PaycheckCalculator.SharedUi/Pages` (Simple Mode + Expert Mode screens) without modification.
- Platform-specific services satisfy the privacy-first contracts in `PaycheckCalculator.Sync.Platform`:
  - `MauiSecureStorage` → Keychain / KeyStore / DPAPI via `SecureStorage.Default`.
  - `MauiLocalDataPathProvider` → `FileSystem.AppDataDirectory` for the encrypted SQLite store.
  - `MauiBiometricUnlock` → stub that reports `IsAvailable=false` until a vetted biometric plugin is wired in.
  - `MauiAppLifecycleEvents` → bridges MAUI `Window` lifecycle to vault-lock / YTD-flush hooks.
- Android `MainActivity` sets `FLAG_SECURE` so paystub data isn't captured in OS screenshots or the recents thumbnail.
- iOS `Info.plist` declares `NSFaceIDUsageDescription` for biometric vault unlock.
- macCatalyst `Entitlements.plist` enables App Sandbox so the encrypted store lives inside the per-app container.

## Requirements coverage

| Spec section | Status |
| --- | --- |
| Federal withholding (Pub 15-T Percentage Method, 2026) | Verified, with explainability + audit |
| Social Security with wage-base tracking | Verified |
| Medicare + Additional Medicare threshold diagnostics | Verified |
| Supplemental wage flat 22% / $1M+ 37% | Verified, with warning emission |
| Pay frequencies incl. 27-biweekly / 53-weekly | Implemented + diagnostic |
| W-4 Steps 2, 3, 4(a), 4(b), 4(c) | Implemented |
| Pre-tax / post-tax deduction classification | 401(k), HSA, FSA, cafeteria, garnishments, charitable, union dues |
| YTD snapshot + delta + rollover | Verified |
| Annual 1040-style projection + refund/due + confidence | Implemented (federal) |
| Per-period optimizer (break-even, target refund, quarterly) | Implemented |
| Schedule C / SE / QBI estimator | Implemented |
| Payday-based budget allocator | Implemented |
| Jurisdiction resolver (manual mode) | Implemented; address/geocoding adapters interface-only |
| State / local rule engine | Framework in place; rules data pending (Manual/Unsupported fallback warning emitted) |
| Versioned signed tax-rule packages | Implemented (RSA SHA-256 signer + verifier shell) |
| E2EE sync envelopes (AES-GCM) | Implemented; tests prove ciphertext leaks no sensitive amounts |
| Minimal API for sync/rules/devices/wrapped keys | Implemented |
| Rule Admin workflow endpoints | Skeleton |
| CSV paycheck export | Implemented |
| Razor UI (Simple Mode) | Implemented with tap-to-expand explainability |

## Building & running

This repo uses the **.NET 11 SDK (Preview 4, `11.0.100-preview.4.26230.115`)**, pinned in `global.json`. The projects target `net11.0` (the MAUI head multi-targets `net11.0-android`/`-ios`/`-maccatalyst`/`-windows`). Because this is a preview SDK, install it from the [.NET 11 download page](https://dotnet.microsoft.com/download/dotnet/11.0) before building.

```bash
# Restore + build the whole solution
dotnet build PaycheckCalculator.slnx

# Run all tests (27 passing)
dotnet test PaycheckCalculator.slnx

# Run the Blazor Web App
dotnet run --project src/PaycheckCalculator.Web --urls http://localhost:5080
# Then open http://localhost:5080/

# Run the minimal API (sync, signed rule package download)
dotnet run --project src/PaycheckCalculator.Api --urls http://localhost:5180
curl http://localhost:5180/v1/rules/2026

# Generate and sign a 2026 federal rule package via the CLI tools
dotnet run --project tools/TaxRuleImporter -- /tmp/federal-2026.json
dotnet run --project tools/RulePackageSigner -- /tmp/federal-2026.json /tmp/federal-2026.signed.json

# Run the golden case runner
dotnet run --project tools/GoldenCaseRunner -- tests/PaycheckCalculator.Tests.Golden/Cases
```

## Privacy and explainability guarantees in code

- All money math is `decimal` (`Money` value object). No binary floating-point in tax calculations.
- Every `TaxLineResult` carries an `ExplainLine[]` with `FormulaId`, `FormulaText`, raw inputs, `RuleSetVersion`, `TaxYear`, `JurisdictionCode`, `RoundingMethod`, and the originating IRS form names.
- Every `CalculationResult` carries a `CalculationAudit` with the engine version, all rule-set versions used, and the rounding policy.
- The state/local engine refuses to silently return zero for an unverified state — it emits a `JurisdictionUnverified` diagnostic so the UI can mark the result as Manual/Unsupported.
- `EnvelopeEncryptor` (Sync) uses AES-GCM with random 96-bit nonces; tests verify ciphertext bytes never contain sample sensitive amounts or employer names.
- The API never sees plaintext financial data. `EncryptedEnvelopeRecord` exposes only `SyncItemId`, `UserId`, `OwnerDeviceId`, `ItemKind`, `ItemVersion`, `UpdatedAtUtc`, `Nonce`, `Ciphertext`, `CiphertextHash`, `Signature`.

## What's stubbed for follow-up phases

- State and local rule data: the engine, support-level enum, and `JurisdictionUnverified` warning are in place; per-state rule JSON still needs to be populated (Phase 4).
- Real geocoding/boundary providers: `IGeocodingProvider` and `IBoundaryProvider` interfaces only.
- Account auth, passkeys, MFA, trusted-device QR pairing: API endpoints are stubbed; backend stores in-memory.
- PDF / Excel / accountant-packet generation: report DTOs in place; renderers pending.
- MAUI shell: see "Solution layout" above.

## License & disclaimer

Estimates only. The 2026 figures in `FederalRule2026.cs` are projections / inflation-adjusted placeholders consumed as a rule data package; replace them with the IRS Publication 15-T release through the rule-update workflow when the official document publishes. Do not use this app as a substitute for advice from a qualified tax professional.
