# PackageUploader

Cross-platform CLI tool and WPF desktop app for uploading game packages to Microsoft Partner Center. Built with .NET 8.0.

## Architecture

Two deliverables from one solution:
- **PackageUploader.Application** — CLI for CI/CD automation (System.CommandLine + Microsoft.Extensions.Hosting)
- **PackageUploader.UI** — WPF MVVM desktop app (Xbox Game Package Manager)

Shared libraries:
- **PackageUploader.ClientApi** — Service layer wrapping Partner Center Ingestion API + Xfus upload service
- **PackageUploader.FileLogger** — Custom async file-based ILoggerProvider with SimpleFile and JSON formatters

## Build & Test

All commands run from `./src`:

```
dotnet restore                               # Restore (uses private NuGet feed via NuGet.config)
dotnet build --no-restore                    # Build all projects
dotnet test --no-build --verbosity normal    # Run all tests (MSTest + Moq)
```

Publish:
```
dotnet publish src/PackageUploader.Application/PackageUploader.Application.csproj --self-contained -r win-x64 -c release
dotnet publish src/PackageUploader.Application/PackageUploader.Application.csproj --self-contained -r linux-x64 -c release
```

## Project Structure

```
src/
  PackageUploader.Application/     CLI entry point, operations, config classes
    Operations/                    One class per CLI command (inherits Operation base)
    Config/                        Strongly-typed config per operation
    Extensions/                    DI registration via extension methods
  PackageUploader.ClientApi/       Partner Center API wrapper
    Client/Ingestion/              REST client for Ingestion API
    Client/Xfus/                   File upload client
    Models/                        Shared data models and config interfaces
  PackageUploader.UI/              WPF desktop app (MVVM)
    View/                          XAML + code-behind pairs (code-behind only sets DataContext)
    ViewModel/                     ICommand properties + RelayCommand logic (all button/click handling)
    Converters/                    IValueConverter implementations
    Providers/                     Service providers for view layer
  PackageUploader.FileLogger/      Custom logging provider
    Formatters/                    SimpleFileFormatter, JsonFileFormatter
schemas/                           JSON schema for operation configs
templates/                         JSON config templates per operation
```

## Key Patterns

- **DI:** Register services via extension methods on IServiceCollection (e.g., `AddPackageUploaderService()`, `AddIngestionAuthentication()`)
- **Configuration:** IOptions<T> with `[OptionsValidator]` partial classes and `[Required]` data annotations; bound via `BindConfiguration()`
- **Error handling:** Custom exception hierarchy from `IngestionClientException`; catch-when filters for HTTP status codes; `StringArgumentException.ThrowIfNullOrWhiteSpace()` for argument validation
- **Logging:** ILogger<T> injection; structured logging with named parameters; timestamp format `"yyyy-MM-dd HH:mm:ss.fff"`
- **HTTP resilience:** Polly with `DecorrelatedJitterBackoffV2` retry + timeout policies via `AddPolicyHandler()`
- **Auth:** Strategy pattern — 11 credential providers (AppSecret, AppCert, Browser, AzureCli, ManagedIdentity, AzurePipelines, etc.)
- **UI:** MVVM — View (.xaml) binds buttons via `Command="{Binding ...}"` → code-behind (.xaml.cs) only sets DataContext → ViewModel declares `ICommand` properties with `RelayCommand` lambdas. Never put logic in code-behind.

## Testing

- Framework: MSTest with `[TestClass]`/`[TestMethod]` attributes
- Mocking: Moq for dependency injection
- Pattern: Arrange/Act/Assert with `[TestInitialize]`/`[TestCleanup]`
- WPF tests use custom `[WpfTestMethod]` attribute (enforces STA thread)
- Integration tests suffixed with `IntegrationTest`

## Package Formats & Tooling

Three package formats are supported:

| Format | Platform | Creation Tool | Upload Method |
|--------|----------|---------------|---------------|
| **XVC** | Xbox console | MakePkg.exe | PackageUploader / Xfus |
| **UWP/MSIXVC** | Windows PC | MakePkg.exe | PackageUploader / Xfus |
| **MSIXVC2** | Next-gen (console + PC) | `makepkg2` | `makepkg2 upload` or loose-file upload |

### makepkg2 (MSIXVC2 tooling)

`makepkg2` is the next-gen packaging tool replacing `MakePkg.exe` for MSIXVC2 packages.
- Source: `Xbox.Apps.Platform` repo (`src/packaging/makepkg2/`) in Azure DevOps (`Xbox.Apps` project)
- Distributed as NuGet: `Microsoft.Xbox.Packaging.Tools.makepkg2`
- Built on .NET 10.0 (win-x64)
- Library: `Microsoft.Xbox.Packaging` — entry interface `IPackagingOperations` (impl: `PackagingOperations`)

**Key CLI commands:**
```
makepkg2 pack /msixvc2 /f layout.xml /d <contentDir> /pd <outputDir> [/encrypt] [/compress brotli] [/noembed] [/skipvalidation]
makepkg2 upload /msixvc2 [/auth Browser] [/branch ...]
packageutil2 extract /package <file>.msixvc /out <outputDir>
```

**MSIXVC2 vs legacy MakePkg:**
- Supports **loose-file upload** (no packaging step required) or packaged upload (`-UsePackage`)
- **Brotli compression** and **encryption** support
- **Embedded vs non-embedded** modes (`/noembed` — boxes stored separately)
- Output: `.msixvc` files (internally zip archives containing `.box` files)
- Auth forwarded via `/auth` flag (Browser, AzureCli, etc.)
- Delta uploads between versions (V1→V2) built into `makepkg2 upload`
- Uses `layout.xml` for chunk organization (same concept as legacy)

**Server-side processing** (Xbox.Xbet.Service repo, `Workflows/MSIXVC2/`):
- `XPackageMsixvc2PublishWorkflow` — Publishing
- `XPackageMsixvc2IngestWorkflow` → `IngestPackageWorkflow` → `IngestBoxWorkflow` — Ingestion pipeline
- `XPackageMsixvc2ScanWorkflow` / `FileScanWorkflow` — Validation scanning
- "Box" = GUID-named `.box` processing unit with tracked `BoxIngestionStatus`

**Reference repos:**
- makepkg2 source: `https://microsoft.visualstudio.com/Xbox.Apps/_git/Xbox.Apps.Platform?path=/src/packaging`
- MSIXVC2 workflows: `https://microsoft.visualstudio.com/Xbox/_git/Xbox.Xbet.Service?path=/src/XPackage/XPackageWorkflow/XPackageWorkflow/Workflows/MSIXVC2`
- Test assets wiki: `https://Microsoft.visualstudio.com/Xbox/_wiki/wikis/Xbox.wiki/314469`

## Domain Glossary

- **Partner Center** — Microsoft portal for game/app publishing
- **Product** — Game or app (identified by ProductId or BigId)
- **Branch** — Version track (branchFriendlyName)
- **Flight** — Preview/test release track (flightName)
- **Market Group** — Regional grouping for package availability
- **XVC** — Xbox Virtual Container (console package format, created by MakePkg.exe)
- **UWP/MSIXVC** — Universal Windows / Modern Xbox package formats (created by MakePkg.exe)
- **MSIXVC2** — Next-gen package format (created by makepkg2), supports loose-file upload and brotli compression
- **makepkg2** — Next-gen CLI tool for creating and uploading MSIXVC2 packages (replaces MakePkg.exe)
- **Xfus** — File upload service used by Partner Center (legacy upload path)
- **Ingestion** — Partner Center backend API
- **EKB** — Encryption key bundle (XVC asset)
- **SubVal** — Submission validation file (XVC asset)
- **Box** — GUID-named `.box` file, the processing unit within an MSIXVC2 package
- **layout.xml** — Chunk organization file defining how content maps into package chunks
- **IPackagingOperations** — Entry interface in `Microsoft.Xbox.Packaging` library for package operations

## Key Rules

- Security features enforced in Directory.Build.props: `HighEntropyVA=true`, `Features=strict`
- NuGet packages come from a private Azure DevOps feed (XboxPackageUploaderFeed) — see `src/NuGet.config`
- CLI operations each have their own Operation class, Config class, and JSON template — keep them in sync
- Config file reference → see Operations.md and schemas/PackageUploaderOperationConfigSchema.json
- MSIXVC2 packaging uses `makepkg2` (not legacy MakePkg.exe) — see `Microsoft.Xbox.Packaging.Tools.makepkg2` NuGet
