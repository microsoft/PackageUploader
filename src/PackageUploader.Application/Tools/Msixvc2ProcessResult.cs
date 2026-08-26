// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace PackageUploader.Application.Tools;

/// <summary>
/// The outcome of a MakePkg.exe run.
/// </summary>
/// <param name="ExitCode">The child process exit code. Zero means the upload succeeded.</param>
/// <param name="UploadedPackageId">
/// CONTRACT: this is null whenever the package identity could not be established with certainty, which
/// covers three cases: MakePkg.exe never printed the identity, it printed something that was not a GUID,
/// or it printed two different identities. Callers must treat null as "unknown" and must never fall back
/// to guessing which package was uploaded, because the identity is used to write availability and
/// pre-download dates and writing them against the wrong package is worse than not writing them at all.
/// </param>
internal sealed record Msixvc2ProcessResult(int ExitCode, string UploadedPackageId);
