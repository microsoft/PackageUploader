// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace PackageUploader.Application.Tools;

/// <summary>
/// Drives MakePkg.exe the same way the UI does (see Msixvc2UploadingViewModel.RunMakePkg2ProcessAsync):
/// no shell execute, redirected stdout/stderr, no console window. Output is streamed through the
/// application logger so console users see live progress, and the child process is killed on cancellation.
/// </summary>
internal sealed class Msixvc2ProcessRunner(ILogger<Msixvc2ProcessRunner> logger) : IMsixvc2ProcessRunner
{
    /// <summary>
    /// MakePkg.exe announces the package it is uploading with an info-level line of the form
    /// "Package Id is &lt;guid&gt;". Verified against makepkg2.exe 2604.405.14000.0, where the line is printed
    /// at default verbosity (no /v required) and before the content transfer begins.
    /// </summary>
    private const string PackageIdMarker = "Package Id is ";

    private readonly ILogger<Msixvc2ProcessRunner> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<Msixvc2ProcessResult> RunAsync(string executablePath, string arguments, CancellationToken ct)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = executablePath,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            },
            EnableRaisingEvents = true,
        };

        // Recursion breaker: MakePkg.exe shells back out to PackageUploader.exe for XVC1 uploads. Stamping
        // the child environment means any PackageUploader.exe started beneath us can see that it is already
        // a delegated invocation and refuse to delegate again, bounding the cycle at a single hop even if
        // the MSIXVC2 format heuristic false-positives. See Msixvc2DelegationGuard.
        process.StartInfo.Environment[Msixvc2DelegationGuard.EnvironmentVariableName] =
            Msixvc2DelegationGuard.EnvironmentVariableValue;

        // Kept local rather than on the instance so that concurrent or repeated runs cannot observe one
        // another's package identity. Both output streams are scanned, so the lock is load-bearing.
        var packageIdLock = new object();
        string uploadedPackageId = null;
        var packageIdAmbiguous = false;

        void CapturePackageId(string line)
        {
            if (!TryParsePackageId(line, out var packageId))
            {
                return;
            }

            lock (packageIdLock)
            {
                if (uploadedPackageId is null)
                {
                    uploadedPackageId = packageId;
                }
                else if (!string.Equals(uploadedPackageId, packageId, StringComparison.OrdinalIgnoreCase))
                {
                    // Two different identities means we cannot say which package the dates belong to.
                    packageIdAmbiguous = true;
                }
            }
        }

        process.OutputDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                _logger.LogInformation("[MakePkg] {Data}", e.Data);
                CapturePackageId(e.Data);
            }
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                _logger.LogWarning("[MakePkg] {Data}", e.Data);
                CapturePackageId(e.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        try
        {
            await process.WaitForExitAsync(ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            KillProcess(process);
            throw;
        }

        if (packageIdAmbiguous)
        {
            _logger.LogWarning(
                "MakePkg.exe reported more than one package id, so the uploaded package cannot be identified.");

            uploadedPackageId = null;
        }

        return new Msixvc2ProcessResult(process.ExitCode, uploadedPackageId);
    }

    /// <summary>
    /// Pulls the package identity out of a MakePkg.exe output line. Deliberately strict: the value must be
    /// a bare GUID in the canonical form, because a loose match risks reporting an identity that is not a
    /// package and having availability dates written against it.
    /// </summary>
    private static bool TryParsePackageId(string line, out string packageId)
    {
        packageId = null;

        var markerIndex = line.IndexOf(PackageIdMarker, StringComparison.OrdinalIgnoreCase);
        if (markerIndex < 0)
        {
            return false;
        }

        var candidate = line[(markerIndex + PackageIdMarker.Length)..].Trim();

        var separatorIndex = candidate.IndexOf(' ');
        if (separatorIndex >= 0)
        {
            candidate = candidate[..separatorIndex];
        }

        if (!Guid.TryParseExact(candidate, "D", out var parsed))
        {
            return false;
        }

        packageId = parsed.ToString();
        return true;
    }

    private void KillProcess(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                _logger.LogWarning("Cancellation requested. Terminating MakePkg.exe.");
                process.Kill(entireProcessTree: true);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to terminate MakePkg.exe after cancellation.");
        }
    }
}
