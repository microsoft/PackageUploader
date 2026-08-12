// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace PackageUploader.Application.Tools;

/// <summary>
/// PLACEHOLDER implementation of <see cref="IMsixvc2UploadToolProvider"/>.
///
/// TODO(GDK-release): replace with IMsixvc2ToolResolver from PackageUploader.ClientApi once PR #&lt;change-1&gt; merges.
/// That PR adds PackageUploader.ClientApi.Tools.IMsixvc2ToolResolver, which performs the real
/// MakePkg.exe (preferred) / makepkg2.exe (fallback) discovery and returns null when unavailable.
/// The rebase is intended to be a one-line change: re-point the IMsixvc2UploadToolProvider
/// registration in HostExtensions.ConfigureServices at an adapter over IMsixvc2ToolResolver.
/// Until then this placeholder reports the capability as always available and relies on
/// MakePkg.exe being resolvable from PATH.
/// </summary>
internal sealed class Msixvc2CapabilityPlaceholder : IMsixvc2UploadToolProvider
{
    private const string PlaceholderExecutable = "MakePkg.exe";

    // TODO(GDK-release): replace with IMsixvc2ToolResolver.IsMsixvc2Supported().
    public bool IsAvailable => true;

    // TODO(GDK-release): replace with IMsixvc2ToolResolver.Resolve()?.ExecutablePath.
    // The replacement must return null/empty (never throw) when Resolve() finds no capable tool,
    // and must share a single Resolve() call with IsAvailable. See IMsixvc2UploadToolProvider's contract.
    public string ExecutablePath => PlaceholderExecutable;
}
