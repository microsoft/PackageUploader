// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;

namespace PackageUploader.Application.Tools;

/// <summary>
/// PLACEHOLDER implementation of <see cref="IMsixvc2UploadToolProvider"/>.
///
/// TODO(GDK-release): replace with IMsixvc2ToolResolver from PackageUploader.ClientApi once PR #&lt;change-1&gt; merges.
/// That PR adds PackageUploader.ClientApi.Tools.IMsixvc2ToolResolver, which performs the real
/// MakePkg.exe (preferred) / makepkg2.exe (fallback) discovery and returns null when unavailable.
/// The rebase is intended to be a one-line change: re-point the IMsixvc2UploadToolProvider
/// registration in HostExtensions.ConfigureServices at an adapter over IMsixvc2ToolResolver.
/// Until then this placeholder reports the capability as always available and relies on
/// MakePkg.exe being resolvable from PATH.
/// </summary>
internal sealed class Msixvc2CapabilityPlaceholder(ILogger<Msixvc2CapabilityPlaceholder> logger) : IMsixvc2UploadToolProvider
{
    private const string PlaceholderExecutable = "MakePkg.exe";
    private const int UploadSourceProbeTimeoutMs = 5000;

    private readonly ILogger<Msixvc2CapabilityPlaceholder> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private bool? _supportsUploadSource;

    // TODO(GDK-release): replace with IMsixvc2ToolResolver.IsMsixvc2Supported().
    public bool IsAvailable => true;

    // TODO(GDK-release): replace with IMsixvc2ToolResolver.Resolve()?.ExecutablePath.
    public string? ExecutablePath => PlaceholderExecutable;

    /// <summary>
    /// Probes the tool for /uploadsource support, mirroring the UI's behaviour
    /// (see Msixvc2UploadViewModel.SupportsUploadSourceFlag). Result is cached per process.
    /// </summary>
    public bool SupportsUploadSource => _supportsUploadSource ??= ProbeUploadSourceSupport();

    private bool ProbeUploadSourceSupport()
    {
        var executablePath = ExecutablePath;
        if (string.IsNullOrEmpty(executablePath))
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
                    Arguments = "supports uploadsource",
                    UseShellExecute = false,
                    RedirectStandardOutput = false,
                    RedirectStandardError = false,
                    CreateNoWindow = true,
                },
            };

            process.Start();
            if (process.WaitForExit(UploadSourceProbeTimeoutMs))
            {
                var supported = process.ExitCode == 0;
                _logger.LogDebug("MakePkg /uploadsource probe: {Result} (exit code {ExitCode}).",
                    supported ? "supported" : "not supported", process.ExitCode);
                return supported;
            }

            try { process.Kill(entireProcessTree: true); } catch { /* best effort */ }
            _logger.LogWarning("MakePkg /uploadsource probe timed out after {TimeoutMs}ms.", UploadSourceProbeTimeoutMs);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to probe MakePkg for /uploadsource support.");
        }

        return false;
    }
}
