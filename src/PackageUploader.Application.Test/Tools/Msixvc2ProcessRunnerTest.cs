// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Moq;
using PackageUploader.Application.Tools;

namespace PackageUploader.Application.Test.Tools;

/// <summary>
/// Exercises the runner against a real child process, because the value under test — the package identity
/// MakePkg.exe reports — is only observable by scanning live process output. A mocked stream would test the
/// parser but not the wiring that feeds it, and the wiring is where a silent regression would hide.
///
/// The identity is what availability and pre-download dates are written against, so a false positive here
/// is worse than no parse at all. These tests are weighted accordingly.
/// </summary>
[TestClass]
public class Msixvc2ProcessRunnerTest
{
    private const string RealPackageId = "e2b5176e-a226-413f-b4d0-32cfbea10047";

    private readonly Mock<ILogger<Msixvc2ProcessRunner>> _loggerMock = new();

    /// <summary>
    /// Echoes the given lines from a real child process. Verified against the actual makepkg2.exe output
    /// shape, which prefixes every line with a timestamp and a log level.
    /// </summary>
    private async Task<Msixvc2ProcessResult> RunEchoAsync(params string[] lines)
    {
        var echo = string.Join(" & ", lines.Select(line => $"echo {line}"));
        return await new Msixvc2ProcessRunner(_loggerMock.Object)
            .RunAsync("cmd.exe", $"/c \"{echo}\"", CancellationToken.None);
    }

    [TestMethod]
    public async Task ReportedPackageId_IsCaptured()
    {
        if (!OperatingSystem.IsWindows())
        {
            Assert.Inconclusive("The echo harness uses cmd.exe.");
        }

        var result = await RunEchoAsync(
            "[10:22:51]     info: Market Group Id is default",
            $"[10:22:56]     info: Package Id is {RealPackageId}",
            "[10:22:56]     info: Xfus Id is 60e309dd-7ba7-4b87-85be-857614150686");

        Assert.AreEqual(0, result.ExitCode);
        Assert.AreEqual(RealPackageId, result.UploadedPackageId);
    }

    [TestMethod]
    public async Task NoReportedPackageId_YieldsNull()
    {
        if (!OperatingSystem.IsWindows())
        {
            Assert.Inconclusive("The echo harness uses cmd.exe.");
        }

        var result = await RunEchoAsync("[10:22:51]     info: Market Group Id is default");

        Assert.AreEqual(0, result.ExitCode);
        Assert.IsNull(result.UploadedPackageId);
    }

    /// <summary>
    /// The Xfus id, draft instance id, CV and ingest job id are all GUIDs on "... is &lt;guid&gt;" lines.
    /// Matching any of them would silently date the wrong thing, so the marker has to be specific rather
    /// than GUID-shaped. Each line is tested alone: together they would trip conflict detection and return
    /// null for the wrong reason, hiding a loose marker.
    /// </summary>
    [TestMethod]
    [DataRow("[10:22:50]     info: Current Draft Instance Id is a484d1f2-1ece-447b-b483-a3abff96ae46")]
    [DataRow("[10:22:56]     info: Xfus Id is 60e309dd-7ba7-4b87-85be-857614150686")]
    [DataRow("[10:22:57]     info: CV is ebab184b-aac6-43c4-812b-94321f9b85d3")]
    [DataRow("[10:23:12]     info: Ingest package job is b60f190e-d7ac-4d04-965f-04f82d0d00c7")]
    [DataRow("[10:23:12]     info: Ingested package")]
    public async Task OtherIdentifiers_AreNotMistakenForThePackageId(string line)
    {
        if (!OperatingSystem.IsWindows())
        {
            Assert.Inconclusive("The echo harness uses cmd.exe.");
        }

        var result = await RunEchoAsync(line);

        Assert.IsNull(result.UploadedPackageId);
    }

    [TestMethod]
    public async Task NonGuidPackageId_IsRejected()
    {
        if (!OperatingSystem.IsWindows())
        {
            Assert.Inconclusive("The echo harness uses cmd.exe.");
        }

        var result = await RunEchoAsync("[10:22:56]     info: Package Id is not-a-guid");

        Assert.IsNull(result.UploadedPackageId);
    }

    /// <summary>
    /// Two different identities means the upload cannot be attributed to one package, so the runner must
    /// report "unknown" rather than picking one.
    /// </summary>
    [TestMethod]
    public async Task ConflictingPackageIds_YieldNull()
    {
        if (!OperatingSystem.IsWindows())
        {
            Assert.Inconclusive("The echo harness uses cmd.exe.");
        }

        var result = await RunEchoAsync(
            $"[10:22:56]     info: Package Id is {RealPackageId}",
            "[10:22:57]     info: Package Id is cd0c5319-ec74-48b5-98f8-4ddc42b9c2af");

        Assert.IsNull(result.UploadedPackageId);
    }

    /// <summary>
    /// The same identity repeated is not a conflict.
    /// </summary>
    [TestMethod]
    public async Task RepeatedIdenticalPackageId_IsStillCaptured()
    {
        if (!OperatingSystem.IsWindows())
        {
            Assert.Inconclusive("The echo harness uses cmd.exe.");
        }

        var result = await RunEchoAsync(
            $"[10:22:56]     info: Package Id is {RealPackageId}",
            $"[10:22:57]     info: Package Id is {RealPackageId}");

        Assert.AreEqual(RealPackageId, result.UploadedPackageId);
    }

    [TestMethod]
    public async Task NonZeroExitCode_IsPropagated()
    {
        if (!OperatingSystem.IsWindows())
        {
            Assert.Inconclusive("The echo harness uses cmd.exe.");
        }

        var result = await new Msixvc2ProcessRunner(_loggerMock.Object)
            .RunAsync("cmd.exe", "/c exit 7", CancellationToken.None);

        Assert.AreEqual(7, result.ExitCode);
        Assert.IsNull(result.UploadedPackageId);
    }
}
