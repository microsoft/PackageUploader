// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;

namespace PackageUploader.ClientApi.Tools;

/// <summary>
/// Default <see cref="IToolPathResolver"/>.
/// </summary>
/// <remarks>
/// Stateless and therefore thread-safe. Nothing is cached, so a tool that appears or is replaced while
/// the process is running is picked up by the next call.
/// </remarks>
public sealed class ToolPathResolver : IToolPathResolver
{
    internal const string GdkToolSubdirectory = "bin";

    private readonly IGdkRootLocator _gdkRootLocator;

    public ToolPathResolver()
        : this(null)
    {
    }

    /// <remarks>
    /// The GDK lookup is seamed rather than exposed: tests substitute it so they never require an
    /// installed GDK, but the environment and registry sources stay an implementation detail.
    /// </remarks>
    internal ToolPathResolver(IGdkRootLocator gdkRootLocator)
    {
        _gdkRootLocator = gdkRootLocator ?? new GdkRootLocator();
    }

    /// <inheritdoc />
    public string Find(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return null;
        }

        // Every stage is guarded on its own, so a source that fails costs only its own candidates and
        // never the stages after it: an unreadable working directory must still leave the GDK and PATH
        // searchable, and a denied registry key must still leave PATH searchable.
        return FindInApplicationDirectory(fileName)
            ?? FindInCurrentDirectory(fileName)
            ?? FindInGdk(fileName)
            ?? FindOnPath(fileName);
    }

    /// <remarks>
    /// The application directory and the current directory deliberately outrank the installed GDK.
    /// Dropping a tool next to the application or into the working directory is the supported way to
    /// pin a hotfix, or a specific version that differs from the one the installed GDK ships, without
    /// having to change the GDK installation itself.
    /// </remarks>
    private static string FindInApplicationDirectory(string fileName)
    {
        try
        {
            return MatchIn(AppContext.BaseDirectory, fileName);
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <inheritdoc cref="FindInApplicationDirectory" />
    private static string FindInCurrentDirectory(string fileName)
    {
        try
        {
            return MatchIn(Directory.GetCurrentDirectory(), fileName);
        }
        catch (Exception)
        {
            // The working directory can be denied or deleted out from under the process.
            return null;
        }
    }

    /// <remarks>
    /// An explicitly installed GDK outranks whatever happens to be on PATH, so a machine with several
    /// copies of a tool resolves predictably.
    /// </remarks>
    private string FindInGdk(string fileName)
    {
        IReadOnlyList<string> gdkRoots;

        try
        {
            gdkRoots = _gdkRootLocator.GetGdkRoots();
        }
        catch (Exception)
        {
            // A failed GDK lookup means "no GDK candidates", not "stop searching".
            return null;
        }

        if (gdkRoots is null)
        {
            return null;
        }

        foreach (string gdkRoot in gdkRoots)
        {
            string match = MatchIn(gdkRoot, GdkToolSubdirectory, fileName);
            if (match is not null)
            {
                return match;
            }
        }

        return null;
    }

    private static string FindOnPath(string fileName)
    {
        string[] directories;

        try
        {
            string pathValue = Environment.GetEnvironmentVariable("PATH");
            if (string.IsNullOrEmpty(pathValue))
            {
                return null;
            }

            directories = pathValue.Split(Path.PathSeparator);
        }
        catch (Exception)
        {
            return null;
        }

        foreach (string directory in directories)
        {
            string match = MatchIn(directory, fileName);
            if (match is not null)
            {
                return match;
            }
        }

        return null;
    }

    private static string MatchIn(string directory, string fileName) =>
        MatchIn(directory, null, fileName);

    /// <summary>
    /// Tests a single candidate. Returns the full path when it exists, otherwise <see langword="null" />.
    /// </summary>
    private static string MatchIn(string directory, string subdirectory, string fileName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                return null;
            }

            string candidate = subdirectory is null
                ? Path.Combine(directory, fileName)
                : Path.Combine(directory, subdirectory, fileName);

            return File.Exists(candidate) ? candidate : null;
        }
        catch (Exception)
        {
            // One unusable candidate - invalid characters, an unreachable share, denied access - must
            // not stop the candidates after it from being tried.
            return null;
        }
    }
}
