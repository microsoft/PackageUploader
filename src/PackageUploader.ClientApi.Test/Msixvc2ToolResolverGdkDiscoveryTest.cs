// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PackageUploader.ClientApi.Tools;

namespace PackageUploader.ClientApi.Test;

/// <summary>
/// Covers GDK-based discovery of the MSIXVC2 packaging tools. The GDK ships both MakePkg.exe and
/// makepkg2.exe in its bin directory, so discovery must serve both.
/// </summary>
/// <remarks>
/// These tests never require an installed GDK: the GDK root lookup is seamed, and PATH is replaced
/// with a controlled directory for the duration of each test.
/// </remarks>
[TestClass]
public class Msixvc2ToolResolverGdkDiscoveryTest
{
    private string _testRoot;
    private string _originalPath;

    [TestInitialize]
    public void Initialize()
    {
        _testRoot = Path.Combine(Path.GetTempPath(), "Msixvc2GdkTest_" + Guid.NewGuid().ToString("N"));
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
    public void Resolve_UsesMakePkgFromTheGdkBinDirectory()
    {
        var gdkRoot = CreateGdkRoot("gdk", "MakePkg.exe");
        var expected = Path.Combine(gdkRoot, "bin", "MakePkg.exe");

        var resolver = CreateResolver(Succeeds(expected), FakeLocator(gdkRoot));

        var tool = resolver.Resolve();

        Assert.IsNotNull(tool);
        Assert.AreEqual(expected, tool.ExecutablePath);
        Assert.IsFalse(tool.IsMakePkg2Fallback);
    }

    [TestMethod]
    public void Resolve_FallsBackToMakePkg2FromTheGdkBinDirectory()
    {
        // Both binaries ship side by side in the GDK; the legacy MakePkg.exe fails the probe and
        // makepkg2.exe answers it, which is exactly the shape of a current GDK install.
        var gdkRoot = CreateGdkRoot("gdk", "MakePkg.exe", "makepkg2.exe");
        var expected = Path.Combine(gdkRoot, "bin", "makepkg2.exe");

        var resolver = CreateResolver(Succeeds(expected), FakeLocator(gdkRoot));

        var tool = resolver.Resolve();

        Assert.IsNotNull(tool);
        Assert.AreEqual(expected, tool.ExecutablePath);
        Assert.IsTrue(tool.IsMakePkg2Fallback);
    }

    [TestMethod]
    public void Resolve_PrefersTheGdkOverPath()
    {
        // The UI resolves GDK before PATH; the command line must agree or the two hosts could run
        // different binaries on the same machine.
        var gdkRoot = CreateGdkRoot("gdk", "MakePkg.exe");
        var pathDirectory = CreateDirectoryWith("on-path", "MakePkg.exe");
        Environment.SetEnvironmentVariable("PATH", pathDirectory);

        var gdkCandidate = Path.Combine(gdkRoot, "bin", "MakePkg.exe");

        // Both candidates would satisfy the probe, so the winner is decided purely by search order.
        var resolver = CreateResolver(_ => new ToolProbeResult(true, 0), FakeLocator(gdkRoot));

        var tool = resolver.Resolve();

        Assert.IsNotNull(tool);
        Assert.AreEqual(gdkCandidate, tool.ExecutablePath);
    }

    [TestMethod]
    public void Resolve_FallsThroughToPath_WhenTheGdkBinDirectoryDoesNotHaveTheTool()
    {
        // A GDK root that exists but predates the MSIXVC2 tools must not stop the search.
        var gdkRoot = CreateGdkRoot("gdk");
        var pathDirectory = CreateDirectoryWith("on-path", "makepkg2.exe");
        Environment.SetEnvironmentVariable("PATH", pathDirectory);

        var expected = Path.Combine(pathDirectory, "makepkg2.exe");

        var resolver = CreateResolver(Succeeds(expected), FakeLocator(gdkRoot));

        var tool = resolver.Resolve();

        Assert.IsNotNull(tool);
        Assert.AreEqual(expected, tool.ExecutablePath);
        Assert.IsTrue(tool.IsMakePkg2Fallback);
    }

    [TestMethod]
    public void Resolve_ReturnsNull_WhenNoGdkIsInstalledAndNothingIsOnPath()
    {
        var resolver = CreateResolver(_ => ToolProbeResult.Failed, FakeLocator());

        Assert.IsNull(resolver.Resolve());
        Assert.IsFalse(resolver.IsMsixvc2Supported());
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

    [TestMethod]
    public void Resolver_DoesNotReferenceTheInternalNuGetPackage()
    {
        // The makepkg2 NuGet feed is internal only, so nothing may point a customer at it.
        var assembly = typeof(Msixvc2ToolResolver).Assembly;

        var offendingConstants = assembly
            .GetTypes()
            .Where(type => type.Namespace == "PackageUploader.ClientApi.Tools")
            .SelectMany(type => type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
            .Where(field => field.IsLiteral && field.FieldType == typeof(string))
            .Select(field => (string)field.GetRawConstantValue())
            .Where(value => value != null &&
                            value.Contains("packaging.tools", StringComparison.OrdinalIgnoreCase))
            .ToList();

        CollectionAssert.AreEqual(new List<string>(), offendingConstants);
    }

    private static Msixvc2ToolResolver CreateResolver(Func<string, ToolProbeResult> probe, IGdkRootLocator locator) =>
        new(null, new FakeProbeRunner(probe), TimeSpan.FromSeconds(1), locator);

    private static Func<string, ToolProbeResult> Succeeds(string supportedPath) =>
        executablePath => string.Equals(executablePath, supportedPath, StringComparison.OrdinalIgnoreCase)
            ? new ToolProbeResult(true, 0)
            : new ToolProbeResult(true, 2);

    private static IGdkRootLocator FakeLocator(params string[] roots) => new StubGdkRootLocator(roots);

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

    private sealed class FakeProbeRunner : IToolProbeRunner
    {
        private readonly Func<string, ToolProbeResult> _probe;

        public FakeProbeRunner(Func<string, ToolProbeResult> probe) => _probe = probe;

        public ToolProbeResult Run(string executablePath, string arguments, TimeSpan timeout)
        {
            Assert.AreEqual("supports uploadsource", arguments);
            return _probe(executablePath);
        }
    }
}
