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
/// Covers the probe and fallback chain over GDK-discovered tools. The GDK ships both MakePkg.exe and
/// makepkg2.exe in its bin directory, so the resolver must probe them in order and pick the capable one.
/// </summary>
/// <remarks>
/// Discovery itself is covered by <c>ToolPathResolverTest</c>. These tests drive the layer above it:
/// the GDK root lookup is seamed and PATH is replaced with a controlled directory, so they never
/// require an installed GDK.
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
    public void Resolve_ReturnsNull_WhenNoGdkIsInstalledAndNothingIsOnPath()
    {
        var resolver = CreateResolver(_ => ToolProbeResult.Failed, FakeLocator());

        Assert.IsNull(resolver.Resolve());
        Assert.IsFalse(resolver.IsMsixvc2Supported());
    }

    [TestMethod]
    public void Resolve_UsesTheInjectedToolPathResolverForDiscovery()
    {
        // Discovery is delegated, so a host that supplies its own IToolPathResolver controls which
        // binaries are probed. This is the seam CHANGE 2's command line adapter inherits.
        var stubPath = Path.Combine(_testRoot, "elsewhere", "MakePkg.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(stubPath));
        File.WriteAllText(stubPath, string.Empty);

        var pathResolver = new RecordingToolPathResolver(stubPath);
        var resolver = new Msixvc2ToolResolver(null, new FakeProbeRunner(Succeeds(stubPath)), TimeSpan.FromSeconds(1), pathResolver);

        var tool = resolver.Resolve();

        Assert.IsNotNull(tool);
        Assert.AreEqual(stubPath, tool.ExecutablePath);
        CollectionAssert.Contains(pathResolver.RequestedFileNames, "MakePkg.exe");
    }

    [TestMethod]
    public void Resolve_PrefersAnExplicitPathOverDiscovery()
    {
        // A non-null hint is authoritative and must suppress discovery entirely, which is how the
        // desktop app avoids searching twice for a tool it has already located.
        var explicitPath = Path.Combine(_testRoot, "explicit", "makepkg2.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(explicitPath));
        File.WriteAllText(explicitPath, string.Empty);

        var pathResolver = new RecordingToolPathResolver(null);
        var resolver = new Msixvc2ToolResolver(null, new FakeProbeRunner(Succeeds(explicitPath)), TimeSpan.FromSeconds(1), pathResolver);

        var tool = resolver.Resolve(string.Empty, explicitPath);

        Assert.IsNotNull(tool);
        Assert.AreEqual(explicitPath, tool.ExecutablePath);
        Assert.IsTrue(tool.IsMakePkg2Fallback);
        Assert.AreEqual(0, pathResolver.RequestedFileNames.Count, "An explicit path must not trigger discovery.");
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
        new(null, new FakeProbeRunner(probe), TimeSpan.FromSeconds(1), new ToolPathResolver(locator));

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

    private sealed class StubGdkRootLocator : IGdkRootLocator
    {
        private readonly IReadOnlyList<string> _roots;

        public StubGdkRootLocator(IReadOnlyList<string> roots) => _roots = roots;

        public IReadOnlyList<string> GetGdkRoots() => _roots;
    }

    /// <summary>
    /// Returns a fixed path for any request and records what was asked for, so a test can assert
    /// whether discovery ran at all.
    /// </summary>
    private sealed class RecordingToolPathResolver : IToolPathResolver
    {
        private readonly string _result;

        public RecordingToolPathResolver(string result) => _result = result;

        public List<string> RequestedFileNames { get; } = new();

        public string Find(string fileName)
        {
            RequestedFileNames.Add(fileName);
            return _result;
        }
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
