// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PackageUploader.ClientApi.Client.Ingestion;
using PackageUploader.ClientApi.Client.Ingestion.Models;
using PackageUploader.ClientApi.Client.Xfus.Uploader;
using PackageUploader.ClientApi.Models;

namespace PackageUploader.ClientApi.Test;

[TestClass]
public class SubmissionValidatorUploadTest
{
    [TestMethod]
    [DataRow(XvcTargetPlatform.PC, true)]
    [DataRow(XvcTargetPlatform.PC, false)]
    [DataRow(XvcTargetPlatform.ConsoleGen8, true)]
    [DataRow(XvcTargetPlatform.ConsoleGen8, false)]
    [DataRow(XvcTargetPlatform.ConsoleGen9, true)]
    [DataRow(XvcTargetPlatform.ConsoleGen9, false)]
    public async Task SuppliedValidator_IsTransferredAndCommittedBeforeDone(XvcTargetPlatform platform, bool delta)
    {
        using var fixture = new UploadFixture(platform, delta);
        await fixture.UploadAsync();

        CollectionAssert.AreEqual(
            new[] { "package-transfer", "processing", "validator-create", "validator-transfer", "validator-commit", "done" },
            fixture.Events);
        fixture.Ingestion.VerifyAll();
        fixture.Transfer.VerifyAll();
    }

    [TestMethod]
    [DataRow("create")]
    [DataRow("transfer")]
    [DataRow("commit")]
    public async Task ValidatorFailure_DoesNotReportDone(string failingStage)
    {
        using var fixture = new UploadFixture(XvcTargetPlatform.ConsoleGen9, true);
        var failure = new HttpRequestException("Simulated validator upload failure.", null, HttpStatusCode.BadGateway);
        if (failingStage == "create")
            fixture.CreateAsset.ThrowsAsync(failure);
        else if (failingStage == "transfer")
            fixture.TransferAsset.ThrowsAsync(failure);
        else
            fixture.CommitAsset.ThrowsAsync(failure);

        var actual = await Assert.ThrowsExactlyAsync<HttpRequestException>(() => fixture.UploadAsync());

        Assert.AreSame(failure, actual);
        Assert.IsFalse(fixture.Events.Contains("done"));
        if (failingStage != "commit")
            fixture.Ingestion.Verify(client => client.CommitPackageAssetAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task OmittedValidator_RemainsOptionalForSharedClient()
    {
        using var fixture = new UploadFixture(XvcTargetPlatform.ConsoleGen9, false);
        await fixture.UploadAsync(includeValidator: false);

        CollectionAssert.AreEqual(new[] { "package-transfer", "processing", "done" }, fixture.Events);
        fixture.Ingestion.Verify(client => client.CreatePackageAssetRequestAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<FileInfo>(),
            GamePackageAssetType.SubmissionValidatorLog, It.IsAny<CancellationToken>()), Times.Never);
    }

    private sealed class UploadFixture : IDisposable
    {
        private readonly string _directory = Path.Combine(Path.GetTempPath(), $"ValidatorUpload-{Guid.NewGuid():N}");
        private readonly string _packagePath;
        private readonly string _validatorPath;
        private readonly bool _delta;
        internal readonly Mock<IIngestionHttpClient> Ingestion = new(MockBehavior.Strict);
        internal readonly Mock<IXfusUploader> Transfer = new(MockBehavior.Strict);
        internal readonly List<string> Events = [];
        internal readonly Moq.Language.Flow.ISetup<IIngestionHttpClient, Task<GamePackageAsset>> CreateAsset;
        internal readonly Moq.Language.Flow.ISetup<IXfusUploader, Task> TransferAsset;
        internal readonly Moq.Language.Flow.ISetup<IIngestionHttpClient, Task<GamePackageAsset>> CommitAsset;

        internal UploadFixture(XvcTargetPlatform platform, bool delta)
        {
            Directory.CreateDirectory(_directory);
            _delta = delta;
            _packagePath = Path.Combine(_directory, platform == XvcTargetPlatform.PC ? "test.msixvc" : "test.xvc");
            _validatorPath = Path.Combine(_directory, "Validator_test.xml");
            // Only metadata is simulated; no distributable console package or server upload is involved.
            byte[] metadata = new byte[4096];
            metadata[1137] = (byte)platform;
            File.WriteAllBytes(_packagePath, metadata);
            File.WriteAllText(_validatorPath, "<project />");
            var package = new GamePackage { Id = "test-package", State = GamePackageState.Processed };
            Ingestion.Setup(client => client.CreatePackageRequestAsync(
                    "test-product", "test-draft", Path.GetFileName(_packagePath), "test-market", true,
                    platform, It.IsAny<CancellationToken>()))
                .ReturnsAsync(package);
            Transfer.Setup(client => client.UploadFileToXfusAsync(
                    It.Is<FileInfo>(file => file.FullName == _packagePath), null, delta,
                    It.IsAny<IProgress<ulong>>(), It.IsAny<CancellationToken>()))
                .Callback(() => Events.Add("package-transfer"))
                .Returns(Task.CompletedTask);
            Ingestion.Setup(client => client.ProcessPackageRequestAsync(
                    "test-product", package, It.IsAny<CancellationToken>()))
                .Callback(() => Events.Add("processing"))
                .ReturnsAsync(package);
            Ingestion.Setup(client => client.GetPackageByIdAsync(
                    "test-product", "test-package", It.IsAny<CancellationToken>()))
                .ReturnsAsync(package);
            CreateAsset = Ingestion.Setup(client => client.CreatePackageAssetRequestAsync(
                "test-product", "test-package", It.Is<FileInfo>(file => file.FullName == _validatorPath),
                GamePackageAssetType.SubmissionValidatorLog, It.IsAny<CancellationToken>()));
            CreateAsset.Callback(() => Events.Add("validator-create"))
                .ReturnsAsync(new GamePackageAsset { Id = "test-validator" });
            TransferAsset = Transfer.Setup(client => client.UploadFileToXfusAsync(
                It.Is<FileInfo>(file => file.FullName == _validatorPath), null, false,
                It.IsAny<IProgress<ulong>>(), It.IsAny<CancellationToken>()));
            TransferAsset.Callback(() => Events.Add("validator-transfer")).Returns(Task.CompletedTask);
            CommitAsset = Ingestion.Setup(client => client.CommitPackageAssetAsync(
                "test-product", "test-package", "test-validator", It.IsAny<CancellationToken>()));
            CommitAsset.Callback(() => Events.Add("validator-commit"))
                .ReturnsAsync(new GamePackageAsset { Id = "test-validator", IsCommitted = true });
        }

        internal Task<GamePackage> UploadAsync(bool includeValidator = true) =>
            new PackageUploaderService(Ingestion.Object, Transfer.Object, NullLogger<PackageUploaderService>.Instance)
                .UploadGamePackageAsync(
                    new GameProduct { ProductId = "test-product" },
                    new GamePackageBranch { Name = "test-branch", CurrentDraftInstanceId = "test-draft" },
                    new GameMarketGroupPackage { MarketGroupId = "test-market" }, _packagePath,
                    new GameAssets { SubValFilePath = includeValidator ? _validatorPath : null },
                    1, _delta, true, new ProgressRecorder(Events), CancellationToken.None);

        public void Dispose() => Directory.Delete(_directory, recursive: true);
    }

    private sealed class ProgressRecorder(List<string> events) : IProgress<PackageUploadingProgress>
    {
        public void Report(PackageUploadingProgress value)
        {
            if (value.Stage == PackageUploadingProgressStage.Done)
                events.Add("done");
        }
    }
}
