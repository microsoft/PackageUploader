---
applyTo: "src/PackageUploader.UI/**"
---

# UI — Xbox Game Package Manager (WPF Desktop)

## Overview

WPF MVVM application for interactive package creation and upload. Uses Microsoft.Extensions.Hosting for DI.

## Architecture

- **View/** — Each view is a `.xaml` + `.xaml.cs` pair (e.g., `MainPageView.xaml` + `MainPageView.xaml.cs`)
- **ViewModel/** — Inherit from `BaseViewModel`; expose `ICommand` properties bound from XAML
- **Providers/** — Service providers bridging view layer to domain logic
- **Converters/** — IValueConverter implementations for XAML binding
- **Model/** — UI-specific data models (PackageModel, ErrorModel, etc.)
- **Utility/** — Helper classes (AuthenticationService, WindowsService, etc.)

## UI Component Pattern (MVVM Command Flow)

Each view is a XAML + code-behind pair. **Code-behind is minimal** — it only sets `DataContext` to the ViewModel and wires `Loaded` events:

```
View/MainPageView.xaml        ← XAML layout, binds buttons via Command="{Binding SignInCommand}"
View/MainPageView.xaml.cs     ← Code-behind: sets DataContext = viewModel (injected via constructor)
ViewModel/MainPageViewModel.cs ← Declares ICommand properties, executes logic via RelayCommand lambdas
```

**Button click flow:**
1. XAML: `<Button Command="{Binding SignInCommand}" />`
2. WPF resolves binding → finds `SignInCommand` on DataContext (the ViewModel)
3. `RelayCommand` executes the lambda defined in the ViewModel constructor
4. ViewModel updates properties → `OnPropertyChanged()` → XAML re-binds

**Never put business logic in code-behind (`.xaml.cs`) — all command logic belongs in the ViewModel.**

## Conventions

- All ViewModels inherit `BaseViewModel` (provides INotifyPropertyChanged, command infrastructure)
- Use `RelayCommand` (action) or `RelayCommand<T>` (with parameter) for ICommand — not raw delegates
- Declare commands as `public ICommand PropertyName { get; }` — initialize in constructor
- Use `Func<bool>` can-execute predicates for conditional button enablement
- Theme support: Light, Dark, High Contrast via XAML resource dictionaries in Resources/Styles/
- Auth uses `InteractiveBrowserCredentialAccessToken` with MSAL caching
- WPF tests require `[WpfTestMethod]` attribute (enforces STA thread)

## Package Format Support

The UI currently supports XVC package creation (via MakePkg.exe) and upload. MSIXVC2 support is being added:

- **MSIXVC2 creation** uses `makepkg2 pack /msixvc2` instead of legacy `MakePkg.exe`
- **MSIXVC2 upload** uses `makepkg2 upload /msixvc2` — supports both loose-file and packaged upload
- makepkg2 is distributed via NuGet: `Microsoft.Xbox.Packaging.Tools.makepkg2`
- The `Microsoft.Xbox.Packaging` library (`IPackagingOperations`) is the C# entry point for programmatic access
- Key differences from legacy: brotli compression, `/noembed` mode, loose-file upload, delta uploads, `/auth` flag forwarding
- Output: `.msixvc` files containing `.box` chunks, organized by `layout.xml`
