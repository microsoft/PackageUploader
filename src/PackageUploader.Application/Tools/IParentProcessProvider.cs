// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace PackageUploader.Application.Tools;

/// <summary>
/// Reports which executable started this PackageUploader process, so the MSIXVC2 delegation path can
/// recognize that it was launched by MakePkg.exe. Seamed as an interface because the real implementation
/// depends on OS process interop, which a unit test cannot arrange.
///
/// CONTRACT — implementations must honor all of the following:
/// <list type="number">
/// <item><see cref="GetParentProcessFileName"/> returns the parent's file name, WITHOUT directory, and with
/// the extension when one is known (for example <c>MakePkg.exe</c>).</item>
/// <item>It returns NULL OR EMPTY when the parent cannot be determined — the parent already exited, the
/// platform is not supported, or the OS denied the query. Callers must tolerate null and must treat it as
/// "unknown", never as "not MakePkg.exe with certainty".</item>
/// <item>It must NEVER THROW. Parent lookup is best-effort diagnostics on a path whose failure mode is a
/// blocked upload; an exception escaping here would turn a missing safety signal into a crash.</item>
/// </list>
/// </summary>
internal interface IParentProcessProvider
{
    /// <summary>
    /// File name of the process that started this one (for example <c>MakePkg.exe</c>), or null/empty when
    /// the parent cannot be determined. Never throws.
    /// </summary>
    string GetParentProcessFileName();
}
