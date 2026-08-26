// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Moq;
using PackageUploader.Application.Tools;

namespace PackageUploader.Application.Test.Tools;

/// <summary>
/// Exercises the runner against a real child process rather than a mocked stream, because what is under
/// test is the wiring — start, redirect, drain both pipes, wait, propagate — and that is exactly what a
/// mocked stream would not cover.
/// </summary>
[TestClass]
public class Msixvc2ProcessRunnerTest
{
    private readonly Mock<ILogger<Msixvc2ProcessRunner>> _loggerMock = new();

    private Task<Msixvc2ProcessResult> RunAsync(string arguments, CancellationToken ct = default) =>
        new Msixvc2ProcessRunner(_loggerMock.Object).RunAsync("cmd.exe", arguments, ct);

    [TestMethod]
    public async Task ZeroExitCode_IsPropagated()
    {
        if (!OperatingSystem.IsWindows())
        {
            Assert.Inconclusive("The harness uses cmd.exe.");
        }

        var result = await RunAsync("/c exit 0");

        Assert.AreEqual(0, result.ExitCode);
    }

    [TestMethod]
    public async Task NonZeroExitCode_IsPropagated()
    {
        if (!OperatingSystem.IsWindows())
        {
            Assert.Inconclusive("The harness uses cmd.exe.");
        }

        var result = await RunAsync("/c exit 7");

        Assert.AreEqual(7, result.ExitCode);
    }

    /// <summary>
    /// Console users only see upload progress because stdout is streamed through the logger, so the
    /// redirection has to actually reach it.
    /// </summary>
    [TestMethod]
    public async Task StandardOutput_IsStreamedToTheLogger()
    {
        if (!OperatingSystem.IsWindows())
        {
            Assert.Inconclusive("The harness uses cmd.exe.");
        }

        await RunAsync("/c echo [10:22:56]     info: Ingested package");

        _loggerMock.VerifyLogInformationContains("Ingested package");
    }

    /// <summary>
    /// stderr is drained as well as stdout. Leaving either pipe unread risks the child blocking on a full
    /// buffer, which would hang the upload rather than fail it.
    /// </summary>
    [TestMethod]
    public async Task StandardError_IsStreamedToTheLogger()
    {
        if (!OperatingSystem.IsWindows())
        {
            Assert.Inconclusive("The harness uses cmd.exe.");
        }

        await RunAsync("/c echo something went wrong 1>&2");

        _loggerMock.VerifyLogWarningContains("something went wrong");
    }

    /// <summary>
    /// Cancellation has to terminate the child, not merely stop awaiting it: an abandoned MakePkg.exe would
    /// keep uploading after the user pressed Ctrl+C.
    /// </summary>
    [TestMethod]
    public async Task Cancellation_KillsTheChildProcess()
    {
        if (!OperatingSystem.IsWindows())
        {
            Assert.Inconclusive("The harness uses cmd.exe.");
        }

        using var cts = new CancellationTokenSource();

        // A child that would otherwise outlive the test by two minutes, so an un-killed process is
        // unambiguous rather than a race.
        var run = RunAsync("/c ping -n 120 127.0.0.1 > nul", cts.Token);

        await cts.CancelAsync();

        await Assert.ThrowsExactlyAsync<TaskCanceledException>(() => run);
        _loggerMock.VerifyLogWarningContains("Terminating MakePkg.exe");
    }
}
