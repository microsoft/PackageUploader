// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;

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
///
/// The environment stamp only covers cycles that PackageUploader.exe itself begins. When MakePkg.exe is the
/// entry point — a user runs it directly and it shells out to us for an XVC1 upload — nothing has stamped
/// our environment, so a third barrier inspects the actual parent process. Seeing MakePkg.exe there while
/// also detecting MSIXVC2 is a contradiction that must not be resolved by delegating back.
/// </summary>
internal interface IMsixvc2DelegationGuard
{
    /// <summary>
    /// True when this PackageUploader process was itself started (directly or indirectly) by a MakePkg.exe
    /// that we delegated to, meaning delegating again would risk an unbounded process cycle.
    /// </summary>
    bool IsDelegatedInvocation { get; }

    /// <summary>
    /// File name of the parent process when PackageUploader was started by a MakePkg executable (for
    /// example <c>MakePkg.exe</c> or <c>makepkg2.exe</c>), otherwise NULL. Null is also returned whenever the
    /// parent cannot be determined, so a null means "not known to be MakePkg", never "definitely not
    /// MakePkg". Never throws.
    /// </summary>
    string GetMakePkgParentProcessName();
}

/// <inheritdoc cref="IMsixvc2DelegationGuard"/>
internal sealed class Msixvc2DelegationGuard(IParentProcessProvider parentProcessProvider) : IMsixvc2DelegationGuard
{
    /// <summary>Environment variable stamped onto the MakePkg.exe child process.</summary>
    public const string EnvironmentVariableName = "PACKAGEUPLOADER_MSIXVC2_DELEGATED";

    /// <summary>Value stamped onto the MakePkg.exe child process.</summary>
    public const string EnvironmentVariableValue = "1";

    /// <summary>
    /// Executable names, without extension, that identify a MakePkg capable of shelling back out to
    /// PackageUploader.exe. Both are covered: <c>makepkg</c> is the tool that performs XVC1 uploads "via the
    /// PackageUploader tool", and <c>makepkg2</c> is the MSIXVC2-capable tool we ourselves delegate to.
    /// </summary>
    private static readonly string[] MakePkgProcessNames = ["makepkg", "makepkg2"];

    private readonly IParentProcessProvider _parentProcessProvider =
        parentProcessProvider ?? throw new ArgumentNullException(nameof(parentProcessProvider));

    public bool IsDelegatedInvocation =>
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(EnvironmentVariableName));

    public string GetMakePkgParentProcessName()
    {
        var parentFileName = _parentProcessProvider.GetParentProcessFileName();

        if (string.IsNullOrWhiteSpace(parentFileName))
        {
            return null;
        }

        // The provider returns an extension when it can read one and a bare process name otherwise, so
        // compare on the stem to accept both "MakePkg.exe" and "MakePkg".
        var stem = Path.GetFileNameWithoutExtension(parentFileName);

        foreach (var makePkgName in MakePkgProcessNames)
        {
            if (string.Equals(stem, makePkgName, StringComparison.OrdinalIgnoreCase))
            {
                return parentFileName;
            }
        }

        return null;
    }
}
