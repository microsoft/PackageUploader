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

        process.OutputDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                _logger.LogInformation("[MakePkg] {Data}", e.Data);
            }
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                _logger.LogWarning("[MakePkg] {Data}", e.Data);
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

        return new Msixvc2ProcessResult(process.ExitCode);
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
