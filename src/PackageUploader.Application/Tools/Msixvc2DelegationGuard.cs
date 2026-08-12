// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace PackageUploader.Application.Tools;

/// <summary>
/// Second, independent barrier against PackageUploader.exe ↔ MakePkg.exe process recursion.
///
/// The primary guard is package-format detection, but <c>PackageFormatDetector.IsLikelyMsixvc2Package</c>
/// is a heuristic: its fallback check scans the trailing bytes of the file for the 4-byte ZIP
/// end-of-central-directory signature, which an encrypted XVC1 tail can contain by chance. A false positive
/// there would be unbounded, because MakePkg.exe shells back out to PackageUploader.exe for XVC1 uploads
/// (the legacy makepkg.exe help text literally describes its upload verb as operating "via the PackageUploader tool"):
///
///   PackageUploader.exe → MakePkg.exe → PackageUploader.exe → MakePkg.exe → ...
///
/// So PackageUploader stamps an environment variable onto every MakePkg.exe child process it starts, and
/// refuses to delegate again if it sees that variable already set in its own environment. Any MakePkg.exe
/// that shells back to us inherits the stamp, which breaks the cycle after exactly one hop regardless of
/// what the format heuristic decides.
/// </summary>
internal interface IMsixvc2DelegationGuard
{
    /// <summary>
    /// True when this PackageUploader process was itself started (directly or indirectly) by a MakePkg.exe
    /// that we delegated to, meaning delegating again would risk an unbounded process cycle.
    /// </summary>
    bool IsDelegatedInvocation { get; }
}

/// <inheritdoc cref="IMsixvc2DelegationGuard"/>
internal sealed class Msixvc2DelegationGuard : IMsixvc2DelegationGuard
{
    /// <summary>Environment variable stamped onto the MakePkg.exe child process.</summary>
    public const string EnvironmentVariableName = "PACKAGEUPLOADER_MSIXVC2_DELEGATED";

    /// <summary>Value stamped onto the MakePkg.exe child process.</summary>
    public const string EnvironmentVariableValue = "1";

    public bool IsDelegatedInvocation =>
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(EnvironmentVariableName));
}
