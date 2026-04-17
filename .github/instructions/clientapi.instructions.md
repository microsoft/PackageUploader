---
applyTo: "src/PackageUploader.ClientApi/**"
---

# ClientApi — Partner Center Service Layer

## Overview

This library wraps the Partner Center Ingestion API and Xfus upload service. It is the shared backend for both the CLI and UI apps.

## Key Interfaces

- `IPackageUploaderService` — 14 async methods for product retrieval, package upload, config updates, removal, import, and publishing
- `IIngestionHttpClient` — REST client for Ingestion API (Client/Ingestion/)
- `IXfusUploader` — File upload handler (Client/Xfus/)

## Upload Paths

Two upload mechanisms exist, depending on package format:

| Format | Upload Mechanism | Handler |
|--------|-----------------|---------|
| **XVC / UWP / MSIXVC** | Xfus upload service | `IXfusUploader` (Client/Xfus/) |
| **MSIXVC2** | `makepkg2 upload /msixvc2` | External process invocation (makepkg2.exe) |

MSIXVC2 upload differs fundamentally from legacy:
- Uses `makepkg2 upload /msixvc2` as an external process (not the Xfus HTTP client)
- Supports loose-file upload (no prior packaging step) or packaged upload (`-UsePackage`)
- Delta uploads between versions are built into `makepkg2 upload`
- Auth forwarded via `/auth` flag (Browser, AzureCli, etc.)
- NuGet: `Microsoft.Xbox.Packaging.Tools.makepkg2`

## Conventions

- Register services via `AddPackageUploaderService()` and `AddIngestionAuthentication()` extension methods
- HTTP clients configured via `AddHttpClient<>()` with Polly retry policies (`DecorrelatedJitterBackoffV2`)
- Auth tokens injected via `IngestionAuthenticationDelegatingHandler`
- Paginated results use `GetAsyncEnumerable()` with `System.Linq.Async`
- All public methods are async and accept `CancellationToken`
- Throw typed exceptions: `ProductNotFoundException`, `PackageBranchNotFoundException`, `SubmissionNotFoundException`
- Log requests/responses at Trace level via `LogRequestVerboseAsync()`
- Config validation uses `[OptionsValidator]` partial classes with `[Required]` annotations
