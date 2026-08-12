// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

namespace PackageUploader.ClientApi.Tools;

/// <summary>
/// A packaging tool that has been verified to support MSIXVC2 upload.
/// </summary>
/// <param name="ExecutablePath">Full path to the executable to invoke.</param>
/// <param name="IsMakePkg2Fallback">
/// True when the resolved tool is the legacy standalone <c>makepkg2.exe</c> (April 2026 GDK preview /
/// Microsoft.Xbox.Packaging.Tools.makepkg2 NuGet package) rather than the current GDK's <c>MakePkg.exe</c>.
/// </param>
public sealed record Msixvc2Tool(string ExecutablePath, bool IsMakePkg2Fallback);
