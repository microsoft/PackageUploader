// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace PackageUploader.ClientApi.Tools;

/// <summary>
/// Default <see cref="IMsixvc2ToolResolver"/>.
/// </summary>
/// <remarks>
/// Stateless and therefore thread-safe. By design nothing is cached: the tool is re-probed on every
/// call so in-place binary updates (for example a GDK upgrade while the app is running) are picked up.
/// </remarks>
public sealed class Msixvc2ToolResolver : IMsixvc2ToolResolver
{
    internal const string MakePkgFileName = "MakePkg.exe";
    internal const string MakePkg2FileName = "makepkg2.exe";
    internal const string SupportsUploadSourceArguments = "supports uploadsource";
    internal const string MakePkg2NuGetPackageId = "microsoft.xbox.packaging.tools.makepkg2";

    private static readonly TimeSpan DefaultProbeTimeout = TimeSpan.FromSeconds(5);

    private readonly IToolProbeRunner _probeRunner;
    private readonly ILogger _logger;
    private readonly TimeSpan _probeTimeout;

    public Msixvc2ToolResolver()
        : this(null, null, null)
    {
    }

    public Msixvc2ToolResolver(ILogger<Msixvc2ToolResolver>? logger)
        : this(logger, null, null)
    {
    }

    public Msixvc2ToolResolver(ILogger<Msixvc2ToolResolver>? logger, IToolProbeRunner? probeRunner, TimeSpan? probeTimeout)
    {
        _logger = logger ?? (ILogger)NullLogger<Msixvc2ToolResolver>.Instance;
        _probeRunner = probeRunner ?? new ProcessToolProbeRunner();
        _probeTimeout = probeTimeout is { } timeout && timeout > TimeSpan.Zero ? timeout : DefaultProbeTimeout;
    }

    /// <inheritdoc />
    public Msixvc2Tool? Resolve() => Resolve(null, null);

    /// <inheritdoc />
    /// <remarks>
    /// A non-null argument (including an empty string) is treated as authoritative and disables
    /// self-discovery for that tool, so hosts that already resolve paths get deterministic behavior.
    /// </remarks>
    public Msixvc2Tool? Resolve(string? makePkgPath, string? makePkg2Path)
    {
        // 1. The current GDK's MakePkg.exe absorbed the makepkg2 capabilities.
        string? makePkgCandidate = makePkgPath is null ? DiscoverOnPath(MakePkgFileName) : NormalizeCandidate(makePkgPath);
        if (makePkgCandidate is not null && ProbeSupportsUploadSource(makePkgCandidate, MakePkgFileName))
        {
            return new Msixvc2Tool(makePkgCandidate, IsMakePkg2Fallback: false);
        }

        // 2. Fall back to the standalone makepkg2.exe shipped by the April 2026 GDK preview.
        string? makePkg2Candidate = makePkg2Path is null ? DiscoverMakePkg2() : NormalizeCandidate(makePkg2Path);
        if (makePkg2Candidate is not null && ProbeSupportsUploadSource(makePkg2Candidate, MakePkg2FileName))
        {
            return new Msixvc2Tool(makePkg2Candidate, IsMakePkg2Fallback: true);
        }

        _logger.LogInformation("No MSIXVC2-capable packaging tool was found. MSIXVC2 upload is unavailable.");
        return null;
    }

    /// <inheritdoc />
    public bool IsMsixvc2Supported() => Resolve() is not null;

    /// <inheritdoc />
    public bool IsMsixvc2Supported(string? makePkgPath, string? makePkg2Path) => Resolve(makePkgPath, makePkg2Path) is not null;

    private static string? NormalizeCandidate(string path) =>
        !string.IsNullOrWhiteSpace(path) && File.Exists(path) ? path : null;

    private bool ProbeSupportsUploadSource(string executablePath, string toolDisplayName)
    {
        ToolProbeResult result = _probeRunner.Run(executablePath, SupportsUploadSourceArguments, _probeTimeout);

        if (!result.Completed)
        {
            _logger.LogInformation(
                "{Tool} uploadsource probe did not complete (missing tool, launch failure, or timeout after {TimeoutSeconds}s) for {Path}.",
                toolDisplayName, _probeTimeout.TotalSeconds, executablePath);
            return false;
        }

        _logger.LogInformation("{Tool} uploadsource probe: {Result} (exit code {ExitCode}) for {Path}.",
            toolDisplayName, result.Succeeded ? "supported" : "not supported", result.ExitCode, executablePath);

        return result.Succeeded;
    }

    /// <summary>
    /// Looks for <paramref name="fileName"/> next to the running application, in the current directory,
    /// and then on PATH. GDK registry lookup is intentionally left to Windows-specific hosts, which pass
    /// their already-resolved path in instead.
    /// </summary>
    private static string? DiscoverOnPath(string fileName)
    {
        try
        {
            string appDirectoryCandidate = Path.Combine(AppContext.BaseDirectory, fileName);
            if (File.Exists(appDirectoryCandidate))
            {
                return appDirectoryCandidate;
            }

            string currentDirectoryCandidate = Path.Combine(Directory.GetCurrentDirectory(), fileName);
            if (File.Exists(currentDirectoryCandidate))
            {
                return currentDirectoryCandidate;
            }

            string? pathValue = Environment.GetEnvironmentVariable("PATH");
            if (string.IsNullOrEmpty(pathValue))
            {
                return null;
            }

            foreach (string directory in pathValue.Split(Path.PathSeparator))
            {
                if (string.IsNullOrWhiteSpace(directory))
                {
                    continue;
                }

                string candidate = Path.Combine(directory, fileName);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
        }
        catch (Exception)
        {
            // Malformed PATH entries and inaccessible directories must not break resolution.
        }

        return null;
    }

    /// <summary>
    /// Discovers makepkg2.exe locally, then falls back to the highest version present in the
    /// Microsoft.Xbox.Packaging.Tools.makepkg2 NuGet package cache.
    /// </summary>
    internal static string? DiscoverMakePkg2()
    {
        string? localPath = DiscoverOnPath(MakePkg2FileName);
        if (localPath is not null)
        {
            return localPath;
        }

        try
        {
            string nugetPackagesDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".nuget", "packages", MakePkg2NuGetPackageId);

            if (!Directory.Exists(nugetPackagesDir))
            {
                return null;
            }

            string? bestPath = null;
            Version? bestVersion = null;

            foreach (string versionDir in Directory.GetDirectories(nugetPackagesDir))
            {
                if (!Version.TryParse(Path.GetFileName(versionDir), out Version? version))
                {
                    continue;
                }

                string candidate = Path.Combine(versionDir, "tools", "any", "win-x64", MakePkg2FileName);
                if (File.Exists(candidate) && (bestVersion is null || version > bestVersion))
                {
                    bestVersion = version;
                    bestPath = candidate;
                }
            }

            return bestPath;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
