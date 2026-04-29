// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Moq;
using PackageUploader.Application.Operations;

namespace PackageUploader.Application.Test.Operations;

internal class TestOperation(ILogger logger, Func<CancellationToken, Task> processAction) : Operation(logger)
{
    protected override Task ProcessAsync(CancellationToken ct) => processAction(ct);
}

[TestClass]
public class OperationTest
{
    private readonly Mock<ILogger> _loggerMock = new();

    [TestMethod]
    public async Task RunAsync_Success_ReturnsZero()
    {
        var operation = new TestOperation(_loggerMock.Object, _ => Task.CompletedTask);

        var result = await operation.RunAsync(CancellationToken.None);

        Assert.AreEqual(0, result);
    }

    [TestMethod]
    public async Task RunAsync_ExceptionThrown_ReturnsThree()
    {
        var operation = new TestOperation(_loggerMock.Object, _ => throw new InvalidOperationException("test error"));

        var result = await operation.RunAsync(CancellationToken.None);

        Assert.AreEqual(3, result);
    }

    [TestMethod]
    public async Task RunAsync_CancellationRequested_ReturnsOne()
    {
        using var cts = new CancellationTokenSource();
        var operation = new TestOperation(_loggerMock.Object, ct =>
        {
            cts.Cancel();
            ct.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        });

        var result = await operation.RunAsync(cts.Token);

        Assert.AreEqual(1, result);
    }

    [TestMethod]
    public async Task RunAsync_ExceptionWithCancellation_ReturnsOne()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var operation = new TestOperation(_loggerMock.Object, _ => throw new OperationCanceledException());

        var result = await operation.RunAsync(cts.Token);

        Assert.AreEqual(1, result);
    }
}
