// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;
using System.Net.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using PackageUploader.ClientApi.Client.Ingestion;
using PackageUploader.ClientApi.Client.Ingestion.Exceptions;
using PackageUploader.ClientApi.Client.Ingestion.Models;
using PackageUploader.ClientApi.Client.Xfus.Models;
using PackageUploader.ClientApi.Client.Xfus.Uploader;
using PackageUploader.ClientApi.Models;

namespace PackageUploader.ClientApi.Test;

[TestClass]
public class ProcessingConflictRecoveryTest
{
    [TestMethod]
    [DataRow(GamePackageState.InProcessing)]
    [DataRow(GamePackageState.Processed)]
    [DataRow(GamePackageState.PendingUpload)]
    [DataRow(GamePackageState.Uploaded)]
    [DataRow(GamePackageState.Unknown)]
    [DataRow(GamePackageState.ProcessFailed)]
    public async Task Conflict_RecoversOnlyWhenServerStateHasAdvanced(GamePackageState serverState)
    {
        using var handler = new ProcessingHandler(serverState);
        using var http = CreateHttpClient(handler);
        var client = CreateProcessingClient(http);
        var package = new GamePackage { Id = "test-package", FileName = "original.msixvc" };

        if (serverState is GamePackageState.InProcessing or GamePackageState.Processed)
        {
            var result = await client.ProcessPackageRequestAsync("test-product", package, CancellationToken.None);
            Assert.AreSame(package, result);
            Assert.AreEqual("test-package", result.Id);
            Assert.AreEqual("original.msixvc", result.FileName);
            Assert.AreEqual(serverState, result.State);
        }
        else
        {
            var error = await Assert.ThrowsExactlyAsync<HttpRequestException>(() =>
                client.ProcessPackageRequestAsync("test-product", package, CancellationToken.None));
            Assert.AreEqual(HttpStatusCode.Conflict, error.StatusCode);
        }

        Assert.AreEqual(1, handler.PutCount);
        Assert.AreEqual(1, handler.GetCount);
    }

    [TestMethod]
    [DataRow(HttpStatusCode.BadRequest)]
    [DataRow(HttpStatusCode.Unauthorized)]
    [DataRow(HttpStatusCode.Forbidden)]
    [DataRow(HttpStatusCode.InternalServerError)]
    public async Task NonConflict_DoesNotAttemptRecovery(HttpStatusCode status)
    {
        using var handler = new ProcessingHandler(GamePackageState.Processed) { PutStatus = status };
        using var http = CreateHttpClient(handler);
        var error = await Assert.ThrowsExactlyAsync<HttpRequestException>(() =>
            CreateProcessingClient(http).ProcessPackageRequestAsync(
                "test-product", new GamePackage { Id = "test-package" }, CancellationToken.None));
        Assert.AreEqual(status, error.StatusCode);
        Assert.AreEqual(0, handler.GetCount);
    }

    [TestMethod]
    public async Task SuccessfulProcessing_DoesNotAttemptRecovery()
    {
        using var handler = new ProcessingHandler(GamePackageState.InProcessing) { PutStatus = HttpStatusCode.OK };
        using var http = CreateHttpClient(handler);
        var result = await CreateProcessingClient(http).ProcessPackageRequestAsync(
            "test-product", new GamePackage { Id = "test-package" }, CancellationToken.None);
        Assert.AreEqual(GamePackageState.InProcessing, result.State);
        Assert.AreEqual(0, handler.GetCount);
    }

    [TestMethod]
    public async Task RecoveryLookupFailure_RemainsAnError()
    {
        using var handler = new ProcessingHandler(GamePackageState.Processed) { GetStatus = HttpStatusCode.NotFound };
        using var http = CreateHttpClient(handler);
        var error = await Assert.ThrowsExactlyAsync<HttpRequestException>(() =>
            CreateProcessingClient(http).ProcessPackageRequestAsync(
                "test-product", new GamePackage { Id = "test-package" }, CancellationToken.None));
        Assert.AreEqual(HttpStatusCode.NotFound, error.StatusCode);
    }

    [TestMethod]
    public async Task EmptyRecoveryLookup_DoesNotSuppressConflict()
    {
        using var handler = new ProcessingHandler(GamePackageState.Processed) { EmptyLookup = true };
        using var http = CreateHttpClient(handler);
        var error = await Assert.ThrowsExactlyAsync<HttpRequestException>(() =>
            CreateProcessingClient(http).ProcessPackageRequestAsync(
                "test-product", new GamePackage { Id = "test-package" }, CancellationToken.None));
        Assert.AreEqual(HttpStatusCode.Conflict, error.StatusCode);
    }

    [TestMethod]
    public async Task CancellationDuringRecovery_IsNotSuppressed()
    {
        using var cancellation = new CancellationTokenSource();
        using var handler = new ProcessingHandler(GamePackageState.Processed) { CancelOnGet = cancellation };
        using var http = CreateHttpClient(handler);
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            CreateProcessingClient(http).ProcessPackageRequestAsync(
                "test-product", new GamePackage { Id = "test-package" }, cancellation.Token));
        Assert.IsTrue(cancellation.IsCancellationRequested);
    }

    [TestMethod]
    [DataRow(GamePackageState.Processed, GamePackageState.Processed)]
    [DataRow(GamePackageState.InProcessing, GamePackageState.Processed)]
    [DataRow(GamePackageState.InProcessing, GamePackageState.ProcessFailed)]
    public async Task RecoveredUpload_PollsAndCommitsValidatorUnlessProcessingFails(
        GamePackageState conflictState, GamePackageState finalState)
    {
        string directory = Path.Combine(Path.GetTempPath(), $"ProcessingRecovery-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            string packagePath = Path.Combine(directory, "test.msixvc");
            string validatorPath = Path.Combine(directory, "Validator_test.xml");
            await File.WriteAllBytesAsync(packagePath, new byte[32]);
            await File.WriteAllTextAsync(validatorPath, "<project />");
            var package = new GamePackage { Id = "test-package" };
            var ingestion = new Mock<IIngestionHttpClient>(MockBehavior.Strict);
            var transfer = new Mock<IXfusUploader>(MockBehavior.Strict);
            using var handler = new ProcessingHandler(conflictState);
            using var http = CreateHttpClient(handler);
            var processingClient = CreateProcessingClient(http);
            ingestion.Setup(client => client.CreatePackageRequestAsync(
                    "test-product", "test-draft", "test.msixvc", "test-market",
                    false, XvcTargetPlatform.NotSpecified, It.IsAny<CancellationToken>()))
                .ReturnsAsync(package);
            ingestion.Setup(client => client.ProcessPackageRequestAsync(
                    "test-product", package, It.IsAny<CancellationToken>()))
                .Returns((string productId, GamePackage uploaded, CancellationToken ct) =>
                    processingClient.ProcessPackageRequestAsync(productId, uploaded, ct));
            ingestion.Setup(client => client.GetPackageByIdAsync(
                    "test-product", "test-package", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GamePackage { Id = "canonical-package", State = finalState });
            transfer.Setup(client => client.UploadFileToXfusAsync(
                    It.Is<FileInfo>(file => file.FullName == packagePath), null, false,
                    It.IsAny<IProgress<ulong>>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            if (finalState is GamePackageState.Processed)
            {
                ingestion.Setup(client => client.CreatePackageAssetRequestAsync(
                        "test-product", "test-package",
                        It.Is<FileInfo>(file => file.FullName == validatorPath),
                        GamePackageAssetType.SubmissionValidatorLog, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new GamePackageAsset { Id = "validator" });
                transfer.Setup(client => client.UploadFileToXfusAsync(
                        It.Is<FileInfo>(file => file.FullName == validatorPath), null,
                        false, It.IsAny<IProgress<ulong>>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);
                ingestion.Setup(client => client.CommitPackageAssetAsync(
                        "test-product", "test-package", "validator", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new GamePackageAsset { Id = "validator", IsCommitted = true });
            }
            var uploader = new PackageUploaderService(
                ingestion.Object, transfer.Object, NullLogger<PackageUploaderService>.Instance);
            Task<GamePackage> Upload() => uploader.UploadGamePackageAsync(
                new GameProduct { ProductId = "test-product" },
                new GamePackageBranch { Name = "test-branch", CurrentDraftInstanceId = "test-draft" },
                new GameMarketGroupPackage { MarketGroupId = "test-market" }, packagePath,
                new GameAssets { SubValFilePath = validatorPath }, 1, false, false, null, CancellationToken.None);
            if (finalState is GamePackageState.ProcessFailed)
            {
                await Assert.ThrowsExactlyAsync<IngestionClientException>(Upload);
            }
            else
            {
                var result = await Upload();
                Assert.AreEqual(GamePackageState.Processed, result.State);
                Assert.AreEqual("test-package", result.Id);
            }
            ingestion.VerifyAll();
            ingestion.VerifyNoOtherCalls();
            transfer.VerifyAll();
            transfer.VerifyNoOtherCalls();
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static HttpClient CreateHttpClient(HttpMessageHandler handler) =>
        new(handler) { BaseAddress = new Uri("https://test.invalid/") };

    private static IngestionHttpClient CreateProcessingClient(HttpClient http) =>
        new(NullLogger<IngestionHttpClient>.Instance, http, null, null);

    private sealed class ProcessingHandler(GamePackageState state) : HttpMessageHandler
    {
        public HttpStatusCode PutStatus { get; init; } = HttpStatusCode.Conflict;
        public HttpStatusCode GetStatus { get; init; } = HttpStatusCode.OK;
        public bool EmptyLookup { get; init; }
        public CancellationTokenSource CancelOnGet { get; init; }
        public int PutCount { get; private set; }
        public int GetCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Assert.AreEqual("/products/test-product/packages/test-package", request.RequestUri.AbsolutePath);
            bool isGet = request.Method == HttpMethod.Get;
            if (isGet)
            {
                GetCount++;
                CancelOnGet?.Cancel();
                cancellationToken.ThrowIfCancellationRequested();
            }
            else
            {
                Assert.AreEqual(HttpMethod.Put, request.Method);
                PutCount++;
            }
            return Task.FromResult(new HttpResponseMessage(isGet ? GetStatus : PutStatus)
            {
                Content = new StringContent(isGet && EmptyLookup ? "null" : isGet || PutStatus == HttpStatusCode.OK
                    ? $"{{\"id\":\"canonical-package\",\"state\":\"{state}\"}}"
                    : "\"Package is being processed or already processed.\"")
            });
        }
    }
}
