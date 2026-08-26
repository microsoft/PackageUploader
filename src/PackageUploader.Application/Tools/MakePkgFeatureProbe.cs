// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace PackageUploader.Application.Tools;

/// <inheritdoc cref="IMakePkgFeatureProbe"/>
internal sealed class MakePkgFeatureProbe(ILogger<MakePkgFeatureProbe> logger) : IMakePkgFeatureProbe
{
    /// <summary>
    /// Exit code MakePkg.exe returns from <c>supports</c> when it does not recognise the feature.
    /// Any non-zero code is treated as "unsupported"; this constant exists to distinguish the tool's
    /// deliberate answer from an unexpected failure when logging.
    /// </summary>
    private const int NotSupportedExitCode = 6;

    private readonly ILogger<MakePkgFeatureProbe> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<bool> SupportsAsync(string executablePath, string feature, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(executablePath) || string.IsNullOrWhiteSpace(feature))
        {
            return false;
        }

        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = executablePath,
                    // The verb takes a bare feature name. Quoted so a feature name is never re-split.
                    Arguments = $"supports \"{feature}\"",
                    UseShellExecute = false,
                    // Output is captured and discarded rather than streamed: this is a silent capability
                    // check, and the answer is carried entirely by the exit code. Redirecting also stops
                    // the probe writing to the console the user is watching for upload progress.
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                },
            };

            process.Start();

            // Both streams are drained. Leaving either unread risks the child blocking on a full pipe and
            // the probe hanging, which would stall an upload that should simply have proceeded or failed.
            var stdoutTask = process.StandardOutput.ReadToEndAsync(ct);
            var stderrTask = process.StandardError.ReadToEndAsync(ct);

            try
            {
                await process.WaitForExitAsync(ct).ConfigureAwait(false);
                await stdoutTask.ConfigureAwait(false);
                await stderrTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                TryKill(process);
                throw;
            }

            if (process.ExitCode == 0)
            {
                return true;
            }

            _logger.LogDebug(
                "'{ExecutablePath} supports {Feature}' returned exit code {ExitCode} ({Interpretation}).",
                executablePath,
                feature,
                process.ExitCode,
                process.ExitCode == NotSupportedExitCode ? "feature not supported" : "unexpected failure");

            return false;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // A tool that cannot even be started is indistinguishable, for our purposes, from one that does
            // not support the feature: either way we must not delegate to it.
            _logger.LogDebug(ex, "Could not probe '{ExecutablePath}' for feature '{Feature}'.", executablePath, feature);
            return false;
        }
    }

    private void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to terminate the MakePkg.exe capability probe after cancellation.");
        }
    }
}
