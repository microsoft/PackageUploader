// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace PackageUploader.UI.Model;

/// <summary>
/// Pulls a single shell visual out of an MSIXVC2 package using the GDK's packageutil.exe.
/// </summary>
/// <remarks>
/// An MSIXVC2 package is a ZIP container, but its payload is not stored as loose entries, so a
/// package's shell visuals cannot be read directly out of the archive. Rather than reimplement the
/// container format here, this defers to packageutil.exe, which the GDK installs in the same bin
/// directory as the MakePkg.exe and makepkg2.exe that the MSIXVC2 upload path already depends on.
///
/// Both tool invocations are cheap - they read only metadata and, for the extract, the requested
/// file - so neither is proportional to the size of the package.
/// </remarks>
internal static class Msixvc2LogoExtractor
{
    internal const string PackageUtilFileName = "packageutil.exe";

    /// <summary>Guards against a hung child process blocking the caller indefinitely.</summary>
    private static readonly TimeSpan ToolTimeout = TimeSpan.FromSeconds(30);

    /// <summary>Shell visuals are small; this only guards against extracting something unexpected.</summary>
    private const long MaxAssetBytes = 32 * 1024 * 1024;

    /// <summary>
    /// Returns the bytes of the first of <paramref name="preferredAssetPaths"/> that the package
    /// actually contains, or null when nothing usable can be extracted. Each path is relative to
    /// MicrosoftGame.config. Never throws: every failure mode - no packageutil.exe, an encrypted
    /// package, a tool error - is a null so that the caller can fall back to the placeholder image.
    /// </summary>
    internal static byte[]? TryExtractAsset(string packageUtilPath, string packagePath, IReadOnlyList<string> preferredAssetPaths)
    {
        try
        {
            if (string.IsNullOrEmpty(packageUtilPath) || !File.Exists(packageUtilPath) || !File.Exists(packagePath))
            {
                return null;
            }

            if (!TryRunPackageUtil(packageUtilPath, ["fileinfo", packagePath], out string listing))
            {
                return null;
            }

            string? assetName = SelectAsset(ParseFileNames(listing), preferredAssetPaths);
            if (assetName is null)
            {
                return null;
            }

            return ExtractAsset(packageUtilPath, packagePath, assetName);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static byte[]? ExtractAsset(string packageUtilPath, string packagePath, string assetName)
    {
        string outputDirectory = Path.Combine(Path.GetTempPath(), "XGPM_" + Guid.NewGuid().ToString("N"));

        try
        {
            Directory.CreateDirectory(outputDirectory);

            if (!TryRunPackageUtil(
                    packageUtilPath,
                    ["extract", "/pd", packagePath, "/file", assetName, "/out", outputDirectory],
                    out _))
            {
                return null;
            }

            // packageutil recreates the file's own directory structure underneath /out.
            string extractedPath = Path.Combine(outputDirectory, assetName);
            if (!File.Exists(extractedPath) || new FileInfo(extractedPath).Length > MaxAssetBytes)
            {
                return null;
            }

            return File.ReadAllBytes(extractedPath);
        }
        finally
        {
            try
            {
                if (Directory.Exists(outputDirectory))
                {
                    Directory.Delete(outputDirectory, recursive: true);
                }
            }
            catch (Exception)
            {
                // A leftover temp directory is not worth failing the preview over.
            }
        }
    }

    /// <summary>
    /// Picks the packaged file matching the earliest usable entry of <paramref name="preferredAssetPaths"/>,
    /// each of which is a path relative to MicrosoftGame.config.
    /// </summary>
    /// <remarks>
    /// The full relative path is matched first, because a package may legitimately contain several
    /// files with the same name in different directories and only the path distinguishes them.
    /// Packaging can also relocate a shell visual - generated tiles are emitted at the package root
    /// regardless of where the source asset lived - so a file name match is kept as a fallback, but
    /// only when exactly one packaged file bears that name. An ambiguous name is skipped rather than
    /// guessed at, which at worst costs the real logo and leaves the placeholder.
    ///
    /// Matching is case-insensitive because the names declared in MicrosoftGame.config need not match
    /// the packaged files' casing, and the packaged spelling is what is returned because packageutil
    /// matches the name it is given exactly.
    /// </remarks>
    internal static string? SelectAsset(IReadOnlyList<string> packagedNames, IReadOnlyList<string> preferredAssetPaths)
    {
        foreach (string preferred in preferredAssetPaths)
        {
            if (string.IsNullOrWhiteSpace(preferred))
            {
                continue;
            }

            string wantedPath = NormalizePath(preferred);
            if (string.IsNullOrEmpty(wantedPath))
            {
                continue;
            }

            foreach (string packaged in packagedNames)
            {
                if (NormalizePath(packaged).Equals(wantedPath, StringComparison.OrdinalIgnoreCase))
                {
                    return packaged;
                }
            }

            string wantedName = Path.GetFileName(wantedPath);
            string? onlyNameMatch = null;

            foreach (string packaged in packagedNames)
            {
                if (!Path.GetFileName(packaged).Equals(wantedName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (onlyNameMatch is not null)
                {
                    onlyNameMatch = null;
                    break;
                }

                onlyNameMatch = packaged;
            }

            if (onlyNameMatch is not null)
            {
                return onlyNameMatch;
            }
        }

        return null;
    }

    /// <summary>
    /// Turns an asset path produced by PartialGameConfigModel back into the path relative to the
    /// config, which is the form the packaged files are named in.
    /// </summary>
    internal static string GetRelativeAssetPath(string configDirectory, string assetPath)
    {
        if (string.IsNullOrEmpty(assetPath))
        {
            return string.Empty;
        }

        if (string.IsNullOrEmpty(configDirectory))
        {
            return assetPath;
        }

        string relativePath;
        try
        {
            relativePath = Path.GetRelativePath(configDirectory, assetPath);
        }
        catch (ArgumentException)
        {
            return Path.GetFileName(assetPath);
        }

        // PartialGameConfigModel roots every asset against the config's own directory. A result that
        // escapes that directory means the config held an absolute path, so only the name is usable.
        if (relativePath.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relativePath))
        {
            return Path.GetFileName(assetPath);
        }

        return relativePath;
    }

    private static string NormalizePath(string path) =>
        path.Replace('/', '\\').TrimStart('.', '\\');

    /// <summary>
    /// Reads the file names out of a "packageutil fileinfo" listing. The listing is a fixed-column
    /// table whose rule line defines the column boundaries, so the rule is used to locate the name
    /// column rather than guessing at the widths.
    /// </summary>
    /// <remarks>
    /// Rows with an empty chunk column describe package user data rather than payload files, and
    /// those cannot be extracted, so they are skipped.
    /// </remarks>
    internal static IReadOnlyList<string> ParseFileNames(string listing)
    {
        var names = new List<string>();

        if (string.IsNullOrEmpty(listing))
        {
            return names;
        }

        string[] lines = listing.Replace("\r\n", "\n").Split('\n');

        int ruleIndex = Array.FindIndex(lines, IsRuleLine);
        if (ruleIndex < 0 || ruleIndex + 1 >= lines.Length)
        {
            return names;
        }

        var columns = GetColumnRanges(lines[ruleIndex]);
        if (columns.Count < 2)
        {
            return names;
        }

        (int chunkStart, int chunkLength) = columns[0];
        (int nameStart, int nameLength) = columns[1];

        for (int i = ruleIndex + 1; i < lines.Length; i++)
        {
            string line = lines[i];
            if (line.Length <= nameStart)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(ReadCell(line, chunkStart, chunkLength)))
            {
                continue;
            }

            string name = ReadCell(line, nameStart, nameLength);
            if (!string.IsNullOrEmpty(name))
            {
                names.Add(name);
            }
        }

        return names;
    }

    private static string ReadCell(string line, int start, int length)
    {
        if (start >= line.Length)
        {
            return string.Empty;
        }

        return line.Substring(start, Math.Min(length, line.Length - start)).Trim();
    }

    private static bool IsRuleLine(string line)
    {
        bool sawRule = false;

        foreach (char c in line)
        {
            if (c == '\u2500')
            {
                sawRule = true;
            }
            else if (c != ' ')
            {
                return false;
            }
        }

        return sawRule;
    }

    private static List<(int Start, int Length)> GetColumnRanges(string ruleLine)
    {
        var ranges = new List<(int, int)>();
        int start = -1;

        for (int i = 0; i <= ruleLine.Length; i++)
        {
            bool isRule = i < ruleLine.Length && ruleLine[i] == '\u2500';

            if (isRule && start < 0)
            {
                start = i;
            }
            else if (!isRule && start >= 0)
            {
                ranges.Add((start, i - start));
                start = -1;
            }
        }

        return ranges;
    }

    private static bool TryRunPackageUtil(string packageUtilPath, string[] arguments, out string standardOutput)
    {
        standardOutput = string.Empty;

        var startInfo = new ProcessStartInfo(packageUtilPath)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            // packageutil writes UTF-8 regardless of the console code page, and the listing's column
            // rule is drawn with box-drawing characters that the parser relies on.
            StandardOutputEncoding = System.Text.Encoding.UTF8,
            StandardErrorEncoding = System.Text.Encoding.UTF8,
            WorkingDirectory = Path.GetTempPath(),
        };

        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo);
        if (process is null)
        {
            return false;
        }

        try
        {
            // Read before waiting so a tool that fills the pipe buffer cannot deadlock against us.
            Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
            Task<string> errorTask = process.StandardError.ReadToEndAsync();

            if (!process.WaitForExit((int)ToolTimeout.TotalMilliseconds))
            {
                process.Kill(entireProcessTree: true);
                return false;
            }

            Task.WaitAll([outputTask, errorTask], ToolTimeout);
            standardOutput = outputTask.IsCompletedSuccessfully ? outputTask.Result : string.Empty;

            return process.ExitCode == 0;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
