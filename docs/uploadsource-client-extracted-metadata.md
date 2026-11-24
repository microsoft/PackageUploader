# UploadSource in ClientExtractedMetaData for XVC1 Uploads — Design Document

**Date:** July 2026  
**Author:** Rafael Hernandez + Copilot  

---

## Problem Statement

PackageUploader already sends an `UploadSource` HTTP header on every Partner Center Ingestion API request, identifying the originating tool (`"PackageUploader"`, `"XGPM"`, or `"makepkg2"`). However, this header only reaches Partner Center — it does not persist with the package metadata and is lost before reaching downstream services like Xbox.Xbet.Service for LogAnalytics telemetry.

The goal is to also include `UploadSource` inside `ClientExtractedMetaData` in the POST body of the package creation request (`POST /products/{productId}/packages`). This way, the value persists with the package and can be consumed by downstream services.

---

## Architecture Overview

### Who Uploads XVC1 Packages?

| Client | Project | How it reaches PackageUploaderService | UploadSource value |
|--------|---------|---------------------------------------|-------------------|
| **XGPM** (Xbox Game Package Manager) | `PackageUploader.UI` | `PackageUploadViewModel` → `UploadGamePackageAsync()` | `"XGPM"` |
| **PackageUploader CLI** | `PackageUploader.Application` | `UploadXvcPackageOperation` → `UploadGamePackageAsync()` | `"PackageUploader"` (default) |
| **makepkg2** | External (via packagingservices.dll) | packagingservices.dll → `PackageUploader.ClientApi` → `UploadGamePackageAsync()` | `"makepkg2"` (pending external integration) |

All three clients converge on the same core method: `PackageUploaderService.UploadGamePackageAsync()`.

### XVC1 Upload Flow

```
PackageUploaderService.UploadGamePackageAsync(product, branch, ..., isXvc: true)
│
├─ 1. Validate files (.xvc, .ekb, symbols, etc.)
│
├─ 2. Create package in Partner Center
│     └─ IngestionHttpClient.CreatePackageRequestAsync(...)
│        └─ IngestionPackageCreationRequestBuilder builds the request body
│           └─ ClientExtractedMetaData { XvcReader { XvcTargetPlatform, GameConfig }, UploadSource }
│        └─ POST /products/{id}/packages
│           - Body: includes ClientExtractedMetaData with UploadSource    ← NEW
│           - Header: UploadSource (already existed, unchanged)
│
├─ 3. Upload binary to XFUS (Xbox File Upload Service)
│     └─ XfusUploader.UploadFileToXfusAsync(...)
│
├─ 4. Mark package as uploaded → wait for processing
│     └─ PUT /products/{id}/packages/{pkgId} (State = "Uploaded")
│
└─ 5. Upload supplemental assets (symbols, SubVal log, etc.)
```

### MSIXVC2 vs XVC1 — Completely Separate Flows

```
MSIXVC2 (XGPM):
  Msixvc2UploadViewModel → shells out to makepkg2.exe with /uploadsource flag
  └─ Does NOT use PackageUploaderService
  └─ Does NOT call CreatePackageRequestAsync
  └─ Does NOT touch ClientExtractedMetaData
  └─ NOT affected by this change

XVC1 (XGPM, CLI, makepkg2):
  PackageUploadViewModel → PackageUploaderService.UploadGamePackageAsync
  └─ CreatePackageRequestAsync → ClientExtractedMetaData { UploadSource }  ← THIS CHANGE
```

---

## Changes Made

### Production Code (6 files)

#### 1. `UploadSourceConfig.cs` — Added `makepkg2` to allowlist
```csharp
public const string MakePkg2Source = "makepkg2";

private static readonly HashSet<string> AllowedValues = new(StringComparer.OrdinalIgnoreCase)
{
    PackageUploaderSource,  // "PackageUploader"
    XgpmSource,             // "XGPM"
    MakePkg2Source,          // "makepkg2"  ← NEW
};
```

#### 2. `PackageUploaderExtensions.cs` — Public constant for external consumers
```csharp
public const string MakePkg2UploadSource = UploadSourceConfig.MakePkg2Source;
```
This allows makepkg2/packagingservices to reference the constant when calling `AddPackageUploaderService(uploadSource: IngestionExtensions.MakePkg2UploadSource)`.

#### 3. `ClientExtractedMetaData.cs` — Added `UploadSource` property
```csharp
public class ClientExtractedMetaData
{
    public XvcReader XvcReader { get; set; }
    public string UploadSource { get; set; }  // ← NEW, nullable
}
```
- When `null`, the JSON serializer omits it (`JsonIgnoreCondition.WhenWritingNull`).
- Valid values: `"PackageUploader"`, `"XGPM"`, `"makepkg2"`, or `null`.

#### 4. `HttpRestClient.cs` — Made `_uploadSource` accessible to subclasses
```csharp
// Changed from:  private readonly string _uploadSource;
// Changed to:
protected readonly string _uploadSource;
```
This allows `IngestionHttpClient` (which extends `HttpRestClient`) to pass the validated upload source to the builder.

#### 5. `IngestionPackageCreationRequestBuilder.cs` — Accepts and propagates `uploadSource`
```csharp
// Constructor now accepts uploadSource (default null for backward compat)
public IngestionPackageCreationRequestBuilder(..., string uploadSource = null)

// CreateClientExtractedMetaData now sets UploadSource
var clientExtractedMetaData = new ClientExtractedMetaData
{
    XvcReader = xvcReader,
    UploadSource = uploadSource,  // ← NEW
};
```

#### 6. `IngestionHttpClient.cs` — Passes `_uploadSource` to the builder
```csharp
// In CreatePackageRequestAsync:
var body = new IngestionPackageCreationRequestBuilder(
    currentDraftInstanceId, fileName, marketGroupId,
    isXvc, xvcTargetPlatform, _uploadSource  // ← NEW parameter
).Build();
```

### Test Code (1 new file, ~30 tests)

**`UploadSourceClientExtractedMetaDataTests.cs`** covers:

| Category | Tests | What they verify |
|----------|-------|-----------------|
| Allowlist | 5 | `makepkg2` accepted, case-insensitive, all three values valid |
| Builder | 7 | UploadSource set for XVC, null when omitted, non-XVC has no metadata, preserves XvcReader |
| Model | 2 | Default is null, round-trips correctly |
| Serialization | 4 | Null omitted from JSON, valid values included, round-trip, no metadata = no UploadSource |
| HTTP flow | 6 | Body contains UploadSource for each config, null/invalid defaults to PackageUploader |
| Header + Body | 1 | Same UploadSource appears in both header and body simultaneously |
| DI | 1 | `AddPackageUploaderService(uploadSource: "makepkg2")` registers correctly |

---

## What Was NOT Changed

- **MSIXVC2 flow** — completely separate, uses makepkg2 CLI with `/uploadsource` flag
- **Public interfaces** — `IPackageUploaderService`, `IIngestionHttpClient` signatures unchanged
- **HTTP header behavior** — `UploadSource` header still sent on every request (unchanged)
- **Non-XVC uploads** — `ClientExtractedMetaData` remains `null` for UWP/MSIX packages

## Pending External Work

For **makepkg2** to send `"makepkg2"` as UploadSource:
- `packagingservices.dll` (external repo) needs to pass `uploadSource: IngestionExtensions.MakePkg2UploadSource` when calling `AddPackageUploaderService()`.
- The "plug" is ready in this repo — no further changes needed here.

## Downstream Consumption

- `ClientExtractedMetaData.UploadSource` will be available in the package creation payload received by Partner Center.
- Publishing to LogAnalytics via Xbox.Xbet.Service is planned for a future session.
