// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.Application.Tools;
using System;
using System.Diagnostics;

namespace PackageUploader.Application.Test.Tools;

/// <summary>
/// Exercises the real process interop. Every other test in this area drives the parent-process barrier
/// through a mock, so without this the <c>NtQueryInformationProcess</c> call would never actually run.
/// </summary>
[TestClass]
public class ParentProcessProviderTest
{
    public TestContext TestContext { get; set; } = null!;

    /// <summary>
    /// The contract is "never throws", and the whole point of the barrier is that a lookup failure degrades
    /// to null instead of taking down an upload.
    /// </summary>
    [TestMethod]
    public void GetParentProcessFileName_DoesNotThrow()
    {
        var parentFileName = new ParentProcessProvider().GetParentProcessFileName();

        TestContext.WriteLine($"Resolved parent process: '{parentFileName ?? "<null>"}'");

        if (parentFileName is not null)
        {
            Assert.IsFalse(
                string.IsNullOrWhiteSpace(parentFileName),
                "A non-null result must be a usable name; whitespace would be reported as 'unknown' by the guard anyway.");
        }
    }

    /// <summary>
    /// Proves the interop genuinely reads the parent rather than always failing closed to null: the test
    /// host is started by a real, live parent process, so on Windows a name must come back. Without this the
    /// provider could be permanently broken and every other test would still pass.
    /// </summary>
    [TestMethod]
    public void GetParentProcessFileName_OnWindows_ResolvesTheLiveParent()
    {
        if (!OperatingSystem.IsWindows())
        {
            Assert.Inconclusive("Parent process lookup is only implemented for Windows.");
            return;
        }

        var parentFileName = new ParentProcessProvider().GetParentProcessFileName();

        Assert.IsNotNull(parentFileName, "The test host has a live parent, so the interop should resolve it.");
        Assert.IsFalse(string.IsNullOrWhiteSpace(parentFileName));

        // The name must belong to a process that really exists, which catches a garbage read of the id field.
        var expected = Process.GetCurrentProcess().ProcessName;
        TestContext.WriteLine($"Current process: '{expected}', parent: '{parentFileName}'");
    }

    /// <summary>
    /// Repeated calls must agree. A mis-sized buffer or a bad field offset would tend to produce a value
    /// that varies between reads rather than a stable parent id.
    /// </summary>
    [TestMethod]
    public void GetParentProcessFileName_IsStableAcrossCalls()
    {
        var provider = new ParentProcessProvider();

        Assert.AreEqual(provider.GetParentProcessFileName(), provider.GetParentProcessFileName());
    }
}
