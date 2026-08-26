// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;

namespace PackageUploader.Application.Tools;

/// <summary>
/// Feature names understood by MakePkg.exe's hidden <c>supports</c> verb.
/// </summary>
internal static class MakePkgFeatures
{
    /// <summary>
    /// Reported by every MakePkg.exe that performs XVC1 uploads itself.
    ///
    /// This is the capability PackageUploader gates delegation on, and the choice is deliberate: the same
    /// MakePkg.exe release that started advertising it is the one that stopped invoking PackageUploader.exe
    /// to perform XVC1 uploads. Probing for it therefore answers two questions at once — "can this tool
    /// upload?" and, more importantly, "is this a tool that would shell back into PackageUploader.exe?".
    /// </summary>
    public const string Xvc1Upload = "xvc1upload";
}

/// <summary>
/// Asks a MakePkg.exe build whether it supports a named feature, via its hidden <c>supports</c> verb.
///
/// CONTRACT: implementations must never throw for an ordinary negative outcome. An older MakePkg.exe, a
/// missing executable, or a tool that fails to start are all reported as <c>false</c>, because "this build
/// does not support the feature" is an expected result that the caller turns into an actionable error
/// rather than an unhandled failure.
/// </summary>
internal interface IMakePkgFeatureProbe
{
    /// <summary>
    /// True when <paramref name="executablePath"/> advertises <paramref name="feature"/>. Never throws,
    /// except for <see cref="System.OperationCanceledException"/> when <paramref name="ct"/> is signalled.
    /// </summary>
    Task<bool> SupportsAsync(string executablePath, string feature, CancellationToken ct);
}
