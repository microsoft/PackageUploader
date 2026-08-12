// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
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

        try
        {
            // 1. Next to the running application.
            string appDirectoryCandidate = Path.Combine(AppContext.BaseDirectory, fileName);
            if (File.Exists(appDirectoryCandidate))
            {
                return appDirectoryCandidate;
            }

            // 2. The current working directory.
            string currentDirectoryCandidate = Path.Combine(Directory.GetCurrentDirectory(), fileName);
            if (File.Exists(currentDirectoryCandidate))
            {
                return currentDirectoryCandidate;
            }

            // 3. Any installed GDK. An explicitly installed GDK outranks whatever happens to be on
            //    PATH, so a machine with several copies of a tool resolves predictably.
            foreach (string gdkRoot in _gdkRootLocator.GetGdkRoots())
            {
                string gdkCandidate = Path.Combine(gdkRoot, GdkToolSubdirectory, fileName);
                if (File.Exists(gdkCandidate))
                {
                    return gdkCandidate;
                }
            }

            // 4. PATH.
            string pathValue = Environment.GetEnvironmentVariable("PATH");
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
}
