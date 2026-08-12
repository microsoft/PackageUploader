// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

namespace PackageUploader.Application.Tools;

/// <summary>
/// Narrow abstraction over "is an MSIXVC2-capable MakePkg.exe available, and where is it?".
/// PackageUploader.Application depends only on this interface so that the underlying capability
/// resolution can be swapped without touching any consuming code.
/// </summary>
internal interface IMsixvc2UploadToolProvider
{
    /// <summary>True when an MSIXVC2-capable packaging tool is installed and usable.</summary>
    bool IsAvailable { get; }

    /// <summary>Full path to the resolved MakePkg.exe (or makepkg2.exe fallback). Null when unavailable.</summary>
    string? ExecutablePath { get; }
}
