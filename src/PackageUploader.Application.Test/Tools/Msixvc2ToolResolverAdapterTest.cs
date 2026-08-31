// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using PackageUploader.Application.Tools;
using PackageUploader.ClientApi.Tools;
using System;

namespace PackageUploader.Application.Test.Tools;

/// <summary>
/// Covers the adapter that replaced the pre-rebase capability placeholder. The placeholder reported
/// the capability as unconditionally available, so the "unavailable" branch of
/// <see cref="IMsixvc2UploadToolProvider"/> had only ever been exercised through a mock of the
/// interface itself. These tests exercise it through the real adapter.
/// </summary>
[TestClass]
public class Msixvc2ToolResolverAdapterTest
{
    private Mock<IMsixvc2ToolResolver> _resolver = null!;

    [TestInitialize]
    public void Initialize()
    {
        _resolver = new Mock<IMsixvc2ToolResolver>(MockBehavior.Strict);
    }

    private Msixvc2ToolResolverAdapter CreateAdapter() =>
        new(_resolver.Object, NullLogger<Msixvc2ToolResolverAdapter>.Instance);

    [TestMethod]
    public void IsAvailable_WhenResolverReturnsNull_IsFalse()
    {
        _resolver.Setup(r => r.Resolve()).Returns((Msixvc2Tool)null!);

        var adapter = CreateAdapter();

        Assert.IsFalse(adapter.IsAvailable);
    }

    [TestMethod]
    public void ExecutablePath_WhenResolverReturnsNull_IsNullOrEmpty()
    {
        _resolver.Setup(r => r.Resolve()).Returns((Msixvc2Tool)null!);

        var adapter = CreateAdapter();

        Assert.IsTrue(string.IsNullOrEmpty(adapter.ExecutablePath));
    }

    [TestMethod]
    public void Members_WhenResolverReturnsTool_ReportItAsAvailable()
    {
        _resolver.Setup(r => r.Resolve()).Returns(new Msixvc2Tool(@"C:\gdk\bin\MakePkg.exe", false));

        var adapter = CreateAdapter();

        Assert.IsTrue(adapter.IsAvailable);
        Assert.AreEqual(@"C:\gdk\bin\MakePkg.exe", adapter.ExecutablePath);
    }

    [TestMethod]
    public void Members_WhenResolverReturnsMakePkg2Fallback_StillReportTheResolvedPath()
    {
        _resolver.Setup(r => r.Resolve()).Returns(new Msixvc2Tool(@"C:\gdk\bin\makepkg2.exe", true));

        var adapter = CreateAdapter();

        Assert.IsTrue(adapter.IsAvailable);
        Assert.AreEqual(@"C:\gdk\bin\makepkg2.exe", adapter.ExecutablePath);
    }

    /// <summary>
    /// The resolver deliberately does not cache and re-probes by launching a candidate executable on
    /// every call, so a two-call adapter would run that probe twice per upload.
    /// </summary>
    [TestMethod]
    public void ReadingBothMembers_ResolvesExactlyOnce()
    {
        _resolver.Setup(r => r.Resolve()).Returns(new Msixvc2Tool(@"C:\gdk\bin\MakePkg.exe", false));

        var adapter = CreateAdapter();

        _ = adapter.IsAvailable;
        _ = adapter.ExecutablePath;
        _ = adapter.IsAvailable;

        _resolver.Verify(r => r.Resolve(), Times.Once);
    }

    [TestMethod]
    public void ReadingBothMembers_WhenUnavailable_StillResolvesExactlyOnce()
    {
        _resolver.Setup(r => r.Resolve()).Returns((Msixvc2Tool)null!);

        var adapter = CreateAdapter();

        _ = adapter.IsAvailable;
        _ = adapter.ExecutablePath;

        _resolver.Verify(r => r.Resolve(), Times.Once);
    }

    /// <summary>
    /// Self-discovery only. The CLI has no path hints to offer, unlike the UI which supplies paths
    /// from its own file pickers.
    /// </summary>
    [TestMethod]
    public void Adapter_UsesSelfDiscovery_NotThePathHintOverload()
    {
        _resolver.Setup(r => r.Resolve()).Returns(new Msixvc2Tool(@"C:\gdk\bin\MakePkg.exe", false));

        var adapter = CreateAdapter();

        _ = adapter.IsAvailable;

        _resolver.Verify(r => r.Resolve(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    /// <summary>
    /// The resolver is documented as never throwing, but UploadXvcPackageOperation is only written to
    /// handle "unavailable" as a clean error. An exception escaping the adapter would be a new failure
    /// mode, so the adapter degrades to unavailable instead.
    /// </summary>
    [TestMethod]
    public void Members_WhenResolverThrows_DegradeToUnavailableInsteadOfPropagating()
    {
        _resolver.Setup(r => r.Resolve()).Throws(new InvalidOperationException("probe exploded"));

        var adapter = CreateAdapter();

        Assert.IsFalse(adapter.IsAvailable);
        Assert.IsTrue(string.IsNullOrEmpty(adapter.ExecutablePath));
    }
}
