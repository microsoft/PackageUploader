// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;

namespace PackageUploader.Application.Tools;

/// <summary>
/// Runs an external process, streaming its output, and returns its exit code.
/// </summary>
internal interface IMsixvc2ProcessRunner
{
    Task<int> RunAsync(string executablePath, string arguments, CancellationToken ct);
}
