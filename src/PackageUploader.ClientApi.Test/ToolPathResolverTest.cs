// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PackageUploader.ClientApi.Tools;

namespace PackageUploader.ClientApi.Test;

/// <summary>
/// Covers the shared tool discovery used by every host: application directory, current directory,
/// GDK <c>bin</c>, then PATH.
/// </summary>
/// <remarks>
/// These tests never require an installed GDK: the GDK root lookup is seamed, and PATH is replaced
/// with a controlled directory for the duration of each test.
/// </remarks>
[TestClass]
public class ToolPathResolverTest
{
    private const string ToolFileName = "MakePkg.exe";

    private string _testRoot;
    private string _originalPath;

    [TestInitialize]
    public void Initialize()
    {
        _testRoot = Path.Combine(Path.GetTempPath(), "ToolPathTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testRoot);

        // Discovery consults the app directory and the current directory before the GDK. A stray real
        // tool in either would silently pre-empt what these tests are asserting, so refuse to run
        // rather than produce a misleading pass or failure.
        foreach (var directory in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
        {
            foreach (var fileName in new[] { "MakePkg.exe", "makepkg2.exe" })
            {
                if (File.Exists(Path.Combine(directory, fileName)))
                {
                    Assert.Inconclusive($"{fileName} is present in {directory}, which pre-empts GDK discovery.");
                }
            }
        }

        _originalPath = Environment.GetEnvironmentVariable("PATH");
        Environment.SetEnvironmentVariable("PATH", Path.Combine(_testRoot, "empty-path"));
    }

    [TestCleanup]
    public void Cleanup()
    {
        Environment.SetEnvironmentVariable("PATH", _originalPath);

        try
        {
            Directory.Delete(_testRoot, recursive: true);
        }
        catch (IOException)
        {
            // Temp cleanup is best effort.
        }
    }

    [TestMethod]
    public void Find_ReturnsTheToolFromTheGdkBinDirectory()
    {
        var gdkRoot = CreateGdkRoot("gdk", ToolFileName);

        var found = CreateResolver(gdkRoot).Find(ToolFileName);

        Assert.AreEqual(Path.Combine(gdkRoot, "bin", ToolFileName), found);
    }

    [TestMethod]
    public void Find_PrefersTheGdkOverPath()
    {
        // An explicitly installed GDK outranks whatever happens to be on PATH. The UI resolved in this
        // order before discovery was shared, and the command line must agree or the two hosts could
        // run different binaries on the same machine.
        var gdkRoot = CreateGdkRoot("gdk", ToolFileName);
        Environment.SetEnvironmentVariable("PATH", CreateDirectoryWith("on-path", ToolFileName));

        var found = CreateResolver(gdkRoot).Find(ToolFileName);

        Assert.AreEqual(Path.Combine(gdkRoot, "bin", ToolFileName), found);
    }

    [TestMethod]
    public void Find_FallsThroughToPath_WhenTheGdkBinDirectoryDoesNotHaveTheTool()
    {
        // A GDK root that exists but predates the tool must not stop the search.
        var gdkRoot = CreateGdkRoot("gdk");
        var pathDirectory = CreateDirectoryWith("on-path", ToolFileName);
        Environment.SetEnvironmentVariable("PATH", pathDirectory);

        var found = CreateResolver(gdkRoot).Find(ToolFileName);

        Assert.AreEqual(Path.Combine(pathDirectory, ToolFileName), found);
    }

    [TestMethod]
    public void Find_SearchesEveryGdkRootInOrder()
    {
        var firstRoot = CreateGdkRoot("gdk-one");
        var secondRoot = CreateGdkRoot("gdk-two", ToolFileName);

        var found = CreateResolver(firstRoot, secondRoot).Find(ToolFileName);

        Assert.AreEqual(Path.Combine(secondRoot, "bin", ToolFileName), found);
    }

    [TestMethod]
    public void Find_ReturnsNull_WhenNoGdkIsInstalledAndNothingIsOnPath()
    {
        Assert.IsNull(CreateResolver().Find(ToolFileName));
    }

    [TestMethod]
    public void Find_ReturnsNull_WhenTheGdkRootDoesNotExistOnDisk()
    {
        var found = CreateResolver(Path.Combine(_testRoot, "not-installed")).Find(ToolFileName);

        Assert.IsNull(found);
    }

    [TestMethod]
    public void Find_ReturnsNull_ForABlankFileName()
    {
        var resolver = CreateResolver();

        Assert.IsNull(resolver.Find(null));
        Assert.IsNull(resolver.Find(string.Empty));
        Assert.IsNull(resolver.Find("   "));
    }

    [TestMethod]
    public void Find_DoesNotThrow_WhenPathIsMalformed()
    {
        // A quoted entry, an empty entry, and characters that are invalid in a path must all be
        // skipped rather than escaping to the caller.
        Environment.SetEnvironmentVariable("PATH", "\"C:\\quoted\";;C:\\bad|entry\0;");

        Assert.IsNull(CreateResolver().Find(ToolFileName));
    }

    [TestMethod]
    public void Find_DoesNotThrow_WhenTheGdkLookupFails()
    {
        var resolver = new ToolPathResolver(new ThrowingGdkRootLocator());

        Assert.IsNull(resolver.Find(ToolFileName));
    }

    [TestMethod]
    public void Find_UsesTheRealGdkLookup_WhenConstructedWithoutASeam()
    {
        // The parameterless constructor is what hosts use. It must work and must not throw whether or
        // not this machine has a GDK, so only the absence of an exception is asserted.
        var resolver = new ToolPathResolver();

        resolver.Find("a-tool-that-does-not-exist.exe");
    }

    [TestMethod]
    public void GdkRootLocator_PrefersTheEnvironmentVariableOverTheRegistry()
    {
        var locator = new GdkRootLocator(
            _ => @"C:\from-env",
            key => key == GdkRootLocator.GdkRegistryKey ? @"C:\from-registry" : null);

        var roots = locator.GetGdkRoots();

        Assert.AreEqual(@"C:\from-env", roots[0]);
        CollectionAssert.Contains(roots.ToList(), @"C:\from-registry");
    }

    [TestMethod]
    public void GdkRootLocator_FallsBackToTheRegistry_WhenTheEnvironmentVariableIsNotSet()
    {
        var locator = new GdkRootLocator(
            _ => null,
            key => key == GdkRootLocator.GdkRegistryKey ? @"C:\from-registry" : null);

        var roots = locator.GetGdkRoots();

        Assert.AreEqual(1, roots.Count);
        Assert.AreEqual(@"C:\from-registry", roots[0]);
    }

    [TestMethod]
    public void GdkRootLocator_FallsBackToTheWow6432NodeMirror()
    {
        var locator = new GdkRootLocator(
            _ => null,
            key => key == GdkRootLocator.GdkWow6432RegistryKey ? @"C:\from-wow6432" : null);

        var roots = locator.GetGdkRoots();

        Assert.AreEqual(1, roots.Count);
        Assert.AreEqual(@"C:\from-wow6432", roots[0]);
    }

    [TestMethod]
    public void GdkRootLocator_ReturnsEmpty_WhenNoSourceHasAGdk()
    {
        var locator = new GdkRootLocator(_ => null, _ => null);

        Assert.AreEqual(0, locator.GetGdkRoots().Count);
    }

    [TestMethod]
    public void GdkRootLocator_DoesNotThrow_WhenASourceFails()
    {
        // Access-denied or a malformed value must never escape into tool resolution.
        var locator = new GdkRootLocator(
            _ => throw new InvalidOperationException("environment blew up"),
            _ => throw new UnauthorizedAccessException("registry blew up"));

        Assert.AreEqual(0, locator.GetGdkRoots().Count);
    }

    [TestMethod]
    public void GdkRootLocator_DefaultSourcesDoNotThrow()
    {
        // Exercises the real environment and registry readers. On a non-Windows host the registry
        // read is skipped by the OperatingSystem.IsWindows() guard rather than throwing.
        var roots = new GdkRootLocator().GetGdkRoots();

        Assert.IsNotNull(roots);
    }

    private static ToolPathResolver CreateResolver(params string[] gdkRoots) =>
        new(new StubGdkRootLocator(gdkRoots));

    private string CreateGdkRoot(string name, params string[] toolFileNames)
    {
        var root = Path.Combine(_testRoot, name);
        var bin = Path.Combine(root, "bin");
        Directory.CreateDirectory(bin);

        foreach (var fileName in toolFileNames)
        {
            File.WriteAllText(Path.Combine(bin, fileName), string.Empty);
        }

        return root;
    }

    private string CreateDirectoryWith(string name, params string[] fileNames)
    {
        var directory = Path.Combine(_testRoot, name);
        Directory.CreateDirectory(directory);

        foreach (var fileName in fileNames)
        {
            File.WriteAllText(Path.Combine(directory, fileName), string.Empty);
        }

        return directory;
    }

    private sealed class StubGdkRootLocator : IGdkRootLocator
    {
        private readonly IReadOnlyList<string> _roots;

        public StubGdkRootLocator(IReadOnlyList<string> roots) => _roots = roots;

        public IReadOnlyList<string> GetGdkRoots() => _roots;
    }

    private sealed class ThrowingGdkRootLocator : IGdkRootLocator
    {
        public IReadOnlyList<string> GetGdkRoots() => throw new UnauthorizedAccessException("locator blew up");
    }
}
