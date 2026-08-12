// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Win32;

namespace PackageUploader.ClientApi.Tools;

/// <summary>
/// Supplies the installation roots of the Microsoft GDK, under which packaging tools live in <c>bin</c>.
/// </summary>
/// <remarks>
/// Seamed so <see cref="Msixvc2ToolResolver"/> can be tested without a GDK installed.
/// </remarks>
internal interface IGdkRootLocator
{
    /// <summary>
    /// Returns candidate GDK installation roots in priority order.
    /// </summary>
    /// <returns>
    /// The roots, or an empty list when no GDK could be located. Never <see langword="null"/>, never throws.
    /// </returns>
    IReadOnlyList<string> GetGdkRoots();
}

/// <summary>
/// Locates the GDK via the <c>GameDK</c> environment variable, then the registry.
/// </summary>
/// <remarks>
/// The registry keys match the ones the WPF host has always used, so command line and UI discovery agree.
/// Registry access is Windows-only and is guarded accordingly; on other platforms only the environment
/// variable is consulted.
/// </remarks>
internal sealed class GdkRootLocator : IGdkRootLocator
{
    internal const string GdkEnvironmentVariableName = "GameDK";
    internal const string GdkInstallPathValueName = "GDKInstallPath";
    internal const string GdkRegistryKey = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\GDK\Installed Roots";
    internal const string GdkWow6432RegistryKey = @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\GDK\Installed Roots";

    private readonly Func<string, string> _environmentVariableReader;
    private readonly Func<string, string> _registryValueReader;

    public GdkRootLocator()
        : this(null, null)
    {
    }

    internal GdkRootLocator(Func<string, string> environmentVariableReader, Func<string, string> registryValueReader)
    {
        _environmentVariableReader = environmentVariableReader ?? Environment.GetEnvironmentVariable;
        _registryValueReader = registryValueReader ?? ReadGdkInstallPath;
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetGdkRoots()
    {
        var roots = new List<string>(3);

        // The environment variable is the cheapest source and is set by the GDK installer.
        AddRoot(roots, Read(_environmentVariableReader, GdkEnvironmentVariableName));

        AddRoot(roots, Read(_registryValueReader, GdkRegistryKey));
        AddRoot(roots, Read(_registryValueReader, GdkWow6432RegistryKey));

        return roots;
    }

    private static string Read(Func<string, string> reader, string key)
    {
        try
        {
            return reader(key);
        }
        catch (Exception)
        {
            // A denied or malformed source must never break tool resolution.
            return null;
        }
    }

    private static void AddRoot(List<string> roots, string root)
    {
        if (!string.IsNullOrWhiteSpace(root) && !roots.Contains(root))
        {
            roots.Add(root);
        }
    }

    private static string ReadGdkInstallPath(string registryKey)
    {
        if (!OperatingSystem.IsWindows())
        {
            return null;
        }

        return Registry.GetValue(registryKey, GdkInstallPathValueName, null) as string;
    }
}
