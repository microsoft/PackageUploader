// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using Microsoft.Win32;

namespace PackageUploader.UI.Utility;

/// <summary>
/// Resolves file paths by searching standard GDK and system locations.
/// </summary>
public static class FileResolver
{
    /// <summary>
    /// Search for a file in several locations in priority order:
    /// 1. Next to the current executable
    /// 2. In the current directory
    /// 3. In the GDK installation (via registry)
    /// 4. In the PATH environment variable
    /// </summary>
    public static string ResolveFilePath(string fileName)
    {
        // Use AppContext.BaseDirectory instead of Assembly.Location for single-file compatibility
        var assemblyDirectory = AppContext.BaseDirectory;

        if (Directory.Exists(assemblyDirectory))
        {
            var nextToExePath = Path.Combine(assemblyDirectory, fileName);

            if (File.Exists(nextToExePath))
            {
                return nextToExePath;
            }
        }

        var currentDirectory = Directory.GetCurrentDirectory();

        var currentDirectoryPath = Path.Combine(currentDirectory, fileName);

        if (File.Exists(currentDirectoryPath))
        {
            return currentDirectoryPath;
        }

        string? gdkFilePath = ResolveFileInGdk(fileName);
        if (!string.IsNullOrEmpty(gdkFilePath))
        {
            return gdkFilePath;
        }

        string? pathFilePath = FindFileInPath(fileName);

        if (File.Exists(pathFilePath))
        {
            return pathFilePath;
        }

        return string.Empty;
    }

    /// <summary>
    /// Search for a file in the GDK installation directories only (via registry).
    /// </summary>
    public static string? ResolveFileInGdk(string fileName)
    {
        string gdkRegistryPath = @"SOFTWARE\Microsoft\GDK\Installed Roots";
        string? gdkPath = Registry.GetValue($@"HKEY_LOCAL_MACHINE\{gdkRegistryPath}", "GDKInstallPath", null) as string;

        if (!string.IsNullOrEmpty(gdkPath))
        {
            var gdkFilePath = Path.Combine(gdkPath, "bin", fileName);
            if (File.Exists(gdkFilePath))
            {
                return gdkFilePath;
            }
        }

        string gdkAltRegistryPath = @"SOFTWARE\WOW6432Node\Microsoft\GDK\Installed Roots";
        string? gdkAltPath = Registry.GetValue($@"HKEY_LOCAL_MACHINE\{gdkAltRegistryPath}", "GDKInstallPath", null) as string;

        if (!string.IsNullOrEmpty(gdkAltPath))
        {
            var gdkFilePath = Path.Combine(gdkAltPath, "bin", fileName);
            if (File.Exists(gdkFilePath))
            {
                return gdkFilePath;
            }
        }

        return null;
    }

    private static string? FindFileInPath(string fileName)
    {
        var pathValue = Environment.GetEnvironmentVariable("PATH");

        if (string.IsNullOrEmpty(pathValue))
        {
            return null;
        }

        var paths = pathValue.Split(Path.PathSeparator);
        foreach (var path in paths)
        {
            var filePath = Path.Combine(path, fileName);
            if (File.Exists(filePath))
            {
                return filePath;
            }
        }
        return null;
    }
}
