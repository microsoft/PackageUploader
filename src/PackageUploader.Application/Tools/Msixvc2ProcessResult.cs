// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace PackageUploader.Application.Tools;

/// <summary>
/// The outcome of a MakePkg.exe run.
/// </summary>
/// <param name="ExitCode">The child process exit code. Zero means the upload succeeded.</param>
internal sealed record Msixvc2ProcessResult(int ExitCode);
