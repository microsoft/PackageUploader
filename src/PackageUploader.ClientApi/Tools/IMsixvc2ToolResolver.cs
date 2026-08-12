// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

namespace PackageUploader.ClientApi.Tools;

/// <summary>
/// Determines which packaging tool, if any, can perform an MSIXVC2 upload.
/// </summary>
/// <remarks>
/// The GDK renamed <c>Makepkg2.exe</c> to <c>MakePkg.exe</c>; the new MakePkg.exe absorbs the
/// makepkg2 capabilities. Resolution order is therefore:
/// <list type="number">
/// <item><description><c>MakePkg.exe supports uploadsource</c> (exit code 0 means supported). A legacy
/// MakePkg.exe fails this, which is the discriminator.</description></item>
/// <item><description>The standalone <c>makepkg2.exe</c> shipped by the April 2026 GDK preview, probed the
/// same way.</description></item>
/// <item><description>Otherwise MSIXVC2 is unavailable.</description></item>
/// </list>
/// Implementations must be thread-safe and must not throw for missing or broken tools.
/// </remarks>
public interface IMsixvc2ToolResolver
{
    /// <summary>
    /// Resolves the MSIXVC2-capable tool using self-discovery (application directory, current directory,
    /// PATH, and the makepkg2 NuGet package cache).
    /// </summary>
    /// <returns>The resolved tool, or <see langword="null"/> when MSIXVC2 is unavailable.</returns>
    Msixvc2Tool? Resolve();

    /// <summary>
    /// Resolves the MSIXVC2-capable tool, preferring caller-supplied paths before self-discovery.
    /// </summary>
    /// <param name="makePkgPath">An already-resolved MakePkg.exe path, or <see langword="null"/> to self-discover.</param>
    /// <param name="makePkg2Path">An already-resolved makepkg2.exe path, or <see langword="null"/> to self-discover.</param>
    /// <returns>The resolved tool, or <see langword="null"/> when MSIXVC2 is unavailable.</returns>
    Msixvc2Tool? Resolve(string? makePkgPath, string? makePkg2Path);

    /// <summary>
    /// Convenience wrapper over <see cref="Resolve()"/>.
    /// </summary>
    bool IsMsixvc2Supported();

    /// <summary>
    /// Convenience wrapper over <see cref="Resolve(string?, string?)"/>.
    /// </summary>
    bool IsMsixvc2Supported(string? makePkgPath, string? makePkg2Path);
}
