// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;

namespace PackageUploader.Application.Tools;

/// <summary>
/// Runs an external process, streaming its output, and reports the exit code together with the identity
/// of the package MakePkg.exe uploaded.
/// </summary>
internal interface IMsixvc2ProcessRunner
{
    Task<Msixvc2ProcessResult> RunAsync(string executablePath, string arguments, CancellationToken ct);
}
