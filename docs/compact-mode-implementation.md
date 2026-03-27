# XGPM Resizable Window & Compact Mode — Implementation Record

**Branch:** `user/kush/resizable-compact-mode`  
**Base:** `elahmed-microsoft/PackageUploader` fork (`main` branch, PR #108)  
**Remote:** `origin` (microsoft/PackageUploader)  
**Date:** March 2026  
**Author:** David Kushmerick + Copilot  

---

## What Was Built

### 1. Resizable Window
- Changed `MainWindow.xaml` from `ResizeMode="NoResize"` (fixed 1200×800) to `ResizeMode="CanResize"` with `MinWidth="600"` `MinHeight="400"`
- Maximize/restore icon toggles with window state
- All views use `ScrollViewer` for overflow handling
- Replaced hardcoded column widths (`Width="607"`, `Width="476"`, etc.) with star-sizing (`3*`, `2*`, `*`) across PackageCreationView, PackageUploadView, Msixvc2UploadView

### 2. Compact Mode Toggle
- **CompactModeProvider** (`Providers/CompactModeProvider.cs`) — persists to `settings.json`, thread-safe with shared `SettingsFileLock`
- **Two size dictionaries** — `Sizes.Normal.xaml` and `Sizes.Compact.xaml` swapped at runtime via `App.xaml.cs:ApplyCompactMode()`
- **Toggle button** in MainWindow title bar (≡ icon) — resizes window to 600×420 (compact) or 1200×800 (standard)
- **BaseViewModel** broadcasts `IsCompactMode` changes to all ViewModel instances via `WeakReference` tracking — no per-VM subscription needed

### 3. Compact Home Screen
- Single-column button-only layout (no cards/borders) at 50% window width, centered
- Info (ⓘ) tooltip buttons using shared `InfoIconGeometry` resource
- All descriptive text hidden, only action buttons + tooltips visible

### 4. Compact Form Views (Make/Upload/Msixvc2)
- Back arrow + page title inline (saves a line)
- Explanatory/description text hidden via `IsCompactMode` visibility bindings
- Preview panel: smaller font (10px), smaller image (40px, top-aligned), tighter padding (3px)
- StoreId/Size/Family and Destination/Market rows stack vertically in compact mode
- Top-aligned preview panels on all three views

### 5. Compact Progress/Upload Screens
- Smaller image, tighter margins via DynamicResource (`ProgressImageMargin`, `ProgressHeadingMargin`, `ProgressSectionMargin`)
- Fixed Msixvc2UploadingView hardcoded margins (was 300px side margins)
- 12px spacing between "Uploading" and "% complete" labels

### 6. Other Changes
- "Destination" field label renamed to "Branch" (en, ja-JP, ko-KR, zh-CN resource files)
- `PercentageConverter` added for percentage-width bindings (guards NaN/Infinity)
- `InfoIconGeometry` shared resource in Styles.xaml (was duplicated 12× inline)
- `ErrorPageView` updated with DynamicResource sizing

---

## Architecture Decisions

| Decision | Rationale |
|----------|-----------|
| Size dictionaries swapped at runtime (like themes) | Matches existing theme system pattern in App.xaml.cs |
| `BaseViewModel.IsCompactMode` with WeakReference broadcasting | All VMs inherit compact mode support automatically — no opt-in needed |
| Shared `SettingsFileLock` on BaseViewModel | Both BaseViewModel and CompactModeProvider write to same settings.json |
| Dual-layout in MainPageView (Grid + StackPanel) | Compact home screen is fundamentally different layout — not just smaller sizes |
| `DynamicResource` for all sizing | Enables runtime switching without restart |

---

## Known Tech Debt

1. **Dual-layout in MainPageView** — standard Grid + compact StackPanel duplicates command bindings. Future: consider ItemsControl + ViewModel collection pattern.
2. **No compact mode developer guide** — new views must manually opt-in to DynamicResource sizing.
3. **Form view description hiding** — each TextBlock has its own visibility binding. Future: consider a shared style with DataTrigger.

---

## Key Files

| File | Role |
|------|------|
| `Providers/CompactModeProvider.cs` | Compact mode state + persistence |
| `ViewModel/BaseViewModel.cs` | IsCompactMode property + WeakReference broadcasting |
| `Resources/Styles/Sizes.Normal.xaml` | Standard sizing values |
| `Resources/Styles/Sizes.Compact.xaml` | Compact sizing values |
| `Resources/Styles/Styles.xaml` | DynamicResource references + InfoIconGeometry |
| `Converters/PercentageConverter.cs` | Width percentage binding converter |
| `App.xaml.cs` | ApplyCompactMode() dictionary swapping |
| `MainWindow.xaml` / `.cs` | Resize support, compact toggle button |

---

## Build & Run

```powershell
cd D:\PackageUploader\src
dotnet restore PackageUploader.UI/PackageUploader.UI.csproj -r win-x64
dotnet build PackageUploader.UI/PackageUploader.UI.csproj -c Debug -r win-x64
# Exe: src\PackageUploader.UI\bin\Debug\net8.0-windows\win-x64\XboxGamePackageManager.exe
```

**Note:** Requires Azure Artifacts Credential Provider for the `XboxPackageUploaderFeed` NuGet source. Install with:
```powershell
iex "& { $(irm https://aka.ms/install-artifacts-credprovider.ps1) } -AddNetfx"
```

## Remotes

| Name | URL |
|------|-----|
| `origin` | https://github.com/microsoft/PackageUploader.git |
| `elahmed` | https://github.com/elahmed-microsoft/PackageUploader.git |
