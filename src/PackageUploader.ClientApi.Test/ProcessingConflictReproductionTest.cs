// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using PackageUploader.ClientApi.Client.Ingestion;
using PackageUploader.ClientApi.Client.Ingestion.Client;
using PackageUploader.ClientApi.Client.Ingestion.Models;
using PackageUploader.ClientApi.Client.Ingestion.TokenProvider;
using PackageUploader.ClientApi.Client.Ingestion.TokenProvider.Models;
using PackageUploader.ClientApi.Client.Xfus.Models;
using PackageUploader.ClientApi.Client.Xfus.Uploader;
using PackageUploader.ClientApi.Models;

namespace PackageUploader.ClientApi.Test;

[TestClass]
public class ProcessingConflictReproductionTest
{
    [TestMethod]
    [DataRow(HttpStatusCode.Conflict, 1, GamePackageState.PendingUpload, false)]
    [DataRow(HttpStatusCode.RequestTimeout, 2, GamePackageState.PendingUpload, false)]
    [DataRow(HttpStatusCode.BadGateway, 2, GamePackageState.PendingUpload, false)]
    [DataRow(HttpStatusCode.ServiceUnavailable, 2, GamePackageState.PendingUpload, false)]
    [DataRow(HttpStatusCode.GatewayTimeout, 2, GamePackageState.PendingUpload, false)]
    [DataRow(HttpStatusCode.Conflict, 1, GamePackageState.Processed, true)]
    [DataRow(HttpStatusCode.Conflict, 1, GamePackageState.InProcessing, true)]
    [DataRow(HttpStatusCode.GatewayTimeout, 2, GamePackageState.Processed, true)]
    [DataRow(HttpStatusCode.ServiceUnavailable, 2, GamePackageState.InProcessing, true)]
    public async Task ProcessingConflict_WithRegisteredRetryPipeline_RecoversOnlyAdvancedState(
        HttpStatusCode firstResponse, int expectedRequests, GamePackageState serverState, bool recover)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string>
            {
                ["IngestionConfig:BaseAddress"] = "https://test.invalid/",
                ["IngestionConfig:MedianFirstRetryDelayMs"] = "1"
            }).Build();
        var tokens = new Mock<IAccessTokenProvider>(MockBehavior.Strict);
        tokens.Setup(provider => provider.GetTokenAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IngestionAccessToken { AccessToken = "synthetic-test-token" });
        using var handler = new ProcessingReplayHandler(firstResponse, serverState);
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
        services.AddSingleton<IAccessTokenProvider>(tokens.Object);
        services.AddSingleton(new UploadSourceConfig());
        services.AddIngestionService();
        services.AddHttpClient<IIngestionHttpClient, IngestionHttpClient>()
            .ConfigurePrimaryHttpMessageHandler(() => handler);
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var client = scope.ServiceProvider.GetRequiredService<IIngestionHttpClient>();

        if (recover)
        {
            var result = await client.ProcessPackageRequestAsync(
                "test-product", new GamePackage { Id = "test-package" }, CancellationToken.None);
            Assert.AreEqual(serverState, result.State);
        }
        else
        {
            var exception = await Assert.ThrowsExactlyAsync<HttpRequestException>(() =>
                client.ProcessPackageRequestAsync(
                    "test-product", new GamePackage { Id = "test-package" }, CancellationToken.None));
            Assert.AreEqual(HttpStatusCode.Conflict, exception.StatusCode);
        }
        Assert.AreEqual(expectedRequests, handler.RequestIds.Count);
        Assert.AreEqual(1, handler.RequestIds.Distinct().Count(),
            "A retry reuses the same logical request ID.");
        tokens.Verify(provider => provider.GetTokenAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    [DataRow(".msixvc", XvcTargetPlatform.PC, true)]
    [DataRow(".msixvc", XvcTargetPlatform.PC, false)]
    [DataRow(".xvc", XvcTargetPlatform.ConsoleGen8, true)]
    [DataRow(".xvc", XvcTargetPlatform.ConsoleGen8, false)]
    [DataRow(".xvc", XvcTargetPlatform.ConsoleGen9, true)]
    [DataRow(".xvc", XvcTargetPlatform.ConsoleGen9, false)]
    public async Task InjectedProcessing409_ThrowsAfterTransfer_ForBothPlatforms(
        string extension, XvcTargetPlatform platform, bool deltaUpload)
    {
        string directory = Path.Combine(Path.GetTempPath(), $"ProcessingConflict-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            // Synthetic metadata fixture, not a valid distributable game package.
            string packagePath = Path.Combine(directory, $"reproduction{extension}");
            byte[] metadata = new byte[4096];
            metadata[1137] = (byte)platform;
            await File.WriteAllBytesAsync(packagePath, metadata);
            string validatorPath = Path.Combine(directory, "Validator_reproduction.xml");
            await File.WriteAllTextAsync(validatorPath, "<project />");

            var product = new GameProduct { ProductId = "test-product" };
            var branch = new GamePackageBranch { Name = "test-branch", CurrentDraftInstanceId = "test-draft" };
            var market = new GameMarketGroupPackage { MarketGroupId = "test-market" };
            var package = new GamePackage { Id = "test-package", FileName = Path.GetFileName(packagePath) };
            var ingestion = new Mock<IIngestionHttpClient>(MockBehavior.Strict);
            var transfer = new Mock<IXfusUploader>(MockBehavior.Strict);
            var progress = new StageRecorder();
            bool transferCompleted = false;

            using var handler = new ProcessingConflictHandler();
            using var http = new HttpClient(handler) { BaseAddress = new Uri("https://test.invalid/") };
            var processingClient = new IngestionHttpClient(
                NullLogger<IngestionHttpClient>.Instance, http, null, null);

            ingestion.Setup(client => client.CreatePackageRequestAsync(
                    product.ProductId, branch.CurrentDraftInstanceId, package.FileName,
                    market.MarketGroupId, true, platform, It.IsAny<CancellationToken>()))
                .ReturnsAsync(package);
            transfer.Setup(client => client.UploadFileToXfusAsync(
                    It.Is<FileInfo>(file => file.FullName == packagePath),
                    It.IsAny<XfusUploadInfo>(), deltaUpload, It.IsAny<IProgress<ulong>>(),
                    It.IsAny<CancellationToken>()))
                .Callback(() => transferCompleted = true)
                .Returns(Task.CompletedTask);
            ingestion.Setup(client => client.ProcessPackageRequestAsync(
                    product.ProductId, package, It.IsAny<CancellationToken>()))
                .Returns((string productId, GamePackage uploadedPackage, CancellationToken ct) =>
                {
                    Assert.IsTrue(transferCompleted, "The processing request must follow the transfer.");
                    return processingClient.ProcessPackageRequestAsync(productId, uploadedPackage, ct);
                });

            var uploader = new PackageUploaderService(
                ingestion.Object, transfer.Object, NullLogger<PackageUploaderService>.Instance);
            var exception = await Assert.ThrowsExactlyAsync<HttpRequestException>(() =>
                uploader.UploadGamePackageAsync(
                    product, branch, market, packagePath,
                    new GameAssets { SubValFilePath = validatorPath },
                    minutesToWaitForProcessing: 60, deltaUpload: deltaUpload, isXvc: true,
                    progress, CancellationToken.None));

            Assert.AreEqual(HttpStatusCode.Conflict, exception.StatusCode);
            Assert.IsTrue(transferCompleted);
            Assert.AreEqual(1, handler.RequestCount);
            CollectionAssert.AreEqual(
                new[]
                {
                    PackageUploadingProgressStage.NotStarted,
                    PackageUploadingProgressStage.ComputingDeltas,
                    PackageUploadingProgressStage.UploadingPackage,
                    PackageUploadingProgressStage.ProcessingPackage
                },
                progress.Stages.ToArray());
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

    private sealed class StageRecorder : IProgress<PackageUploadingProgress>
    {
        public List<PackageUploadingProgressStage> Stages { get; } = [];

        public void Report(PackageUploadingProgress value) => Stages.Add(value.Stage);
    }

    private sealed class ProcessingConflictHandler : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Method == HttpMethod.Get)
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"id\":\"test-package\",\"state\":\"PendingUpload\"}")
                };
            }
            RequestCount++;
            Assert.AreEqual(HttpMethod.Put, request.Method);
            Assert.AreEqual("/products/test-product/packages/test-package", request.RequestUri.AbsolutePath);
            using var body = JsonDocument.Parse(await request.Content.ReadAsStringAsync(cancellationToken));
            Assert.AreEqual("Uploaded", body.RootElement.GetProperty("State").GetString());
            return new HttpResponseMessage(HttpStatusCode.Conflict)
            {
                Content = new StringContent("\"Package is being processed or already processed.\"")
            };
        }
    }

    private sealed class ProcessingReplayHandler(HttpStatusCode firstResponse, GamePackageState serverState) : HttpMessageHandler
    {
        public List<string> RequestIds { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Method == HttpMethod.Get)
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent($"{{\"id\":\"test-package\",\"state\":\"{serverState}\"}}")
                };
            }
            Assert.AreEqual(HttpMethod.Put, request.Method);
            Assert.AreEqual("/products/test-product/packages/test-package", request.RequestUri.AbsolutePath);
            using var body = JsonDocument.Parse(await request.Content.ReadAsStringAsync(cancellationToken));
            Assert.AreEqual("Uploaded", body.RootElement.GetProperty("State").GetString());
            RequestIds.Add(request.Headers.GetValues("Request-ID").Single());
            return new HttpResponseMessage(RequestIds.Count == 1 ? firstResponse : HttpStatusCode.Conflict)
            {
                Content = new StringContent("\"Package is being processed or already processed.\"")
            };
        }
    }
}
