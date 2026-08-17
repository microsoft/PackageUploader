// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;

namespace PackageUploader.ClientApi.Tools;

/// <summary>
/// Outcome of a capability probe.
/// </summary>
/// <param name="Completed">False when the process could not be started or did not exit within the timeout.</param>
/// <param name="ExitCode">Process exit code when <see cref="Completed"/> is true; otherwise undefined.</param>
public readonly record struct ToolProbeResult(bool Completed, int ExitCode)
{
    public static ToolProbeResult Failed => new(false, -1);

    public bool Succeeded => Completed && ExitCode == 0;
}

/// <summary>
/// Runs a short-lived capability probe. Abstracted so the resolver can be unit tested without spawning processes.
/// </summary>
public interface IToolProbeRunner
{
    ToolProbeResult Run(string executablePath, string arguments, TimeSpan timeout);
}

/// <summary>
/// Default <see cref="IToolProbeRunner"/> that launches the tool with no window and no shell execution.
/// Never throws: process start failures are reported as <see cref="ToolProbeResult.Failed"/>.
/// </summary>
public sealed class ProcessToolProbeRunner : IToolProbeRunner
{
    public ToolProbeResult Run(string executablePath, string arguments, TimeSpan timeout)
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = executablePath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = false,
                    RedirectStandardError = false,
                    CreateNoWindow = true,
                }
            };

            process.Start();

            if (process.WaitForExit((int)timeout.TotalMilliseconds))
            {
                return new ToolProbeResult(true, process.ExitCode);
            }

            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch
            {
                // Best effort.
            }

            return ToolProbeResult.Failed;
        }
        catch
        {
            return ToolProbeResult.Failed;
        }
    }
}
