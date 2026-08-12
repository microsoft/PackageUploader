// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace PackageUploader.Application.Tools;

/// <summary>
/// Narrow abstraction over "is an MSIXVC2-capable MakePkg.exe available, and where is it?".
/// PackageUploader.Application depends only on this interface so that the underlying capability
/// resolution can be swapped without touching any consuming code.
///
/// CONTRACT — implementations must honor all of the following. These are stated here because this
/// project does not compile with nullable reference types, so the compiler cannot express them:
/// <list type="number">
/// <item><see cref="IsAvailable"/> is false exactly when no MSIXVC2-capable tool could be found.</item>
/// <item><see cref="ExecutablePath"/> is NULL OR EMPTY whenever <see cref="IsAvailable"/> is false, and
/// is a usable full path whenever it is true. Callers must tolerate a null path.</item>
/// <item>Neither member may THROW when no tool is available. "Unavailable" is an ordinary, expected
/// outcome that <see cref="Operations.UploadXvcPackageOperation"/> reports as a clean, actionable error;
/// an exception escaping either member would instead surface as an unhandled failure.</item>
/// <item>Both members should reflect a SINGLE underlying resolution. An implementation that resolves
/// separately per member can double any probing work and can disagree with itself between the two reads,
/// so resolve once and have both members report that one result.</item>
/// </list>
/// </summary>
internal interface IMsixvc2UploadToolProvider
{
    /// <summary>
    /// True when an MSIXVC2-capable packaging tool is installed and usable. Never throws;
    /// "not available" is reported as false rather than as an exception.
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Full path to the resolved MakePkg.exe (or makepkg2.exe fallback).
    /// NULL OR EMPTY when <see cref="IsAvailable"/> is false — callers must check before use.
    /// Never throws.
    /// </summary>
    string ExecutablePath { get; }
}
