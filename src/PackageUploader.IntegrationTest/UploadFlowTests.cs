// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using PackageUploader.ClientApi.Client.Ingestion.Models;
using PackageUploader.IntegrationTest.Fixtures;
using PackageUploader.IntegrationTest.Infrastructure;
using System.Threading;

namespace PackageUploader.IntegrationTest;

/// <summary>
/// End-to-end upload flow that ties both fakes together: the real service creates a package against
/// the Ingestion fake, uploads the file to the XFUS fake (no-delta), then processes and polls the
/// package to completion.
/// </summary>
[TestClass]
public sealed class UploadFlowTests : IntegrationTestBase
{
    [TestMethod]
    public async Task UploadGamePackage_NoDelta_CompletesThroughIngestionAndXfus()
    {
        using var host = CreateMockServerHost();
        using var packageFile = SyntheticPackageFile.Create(sizeInBytes: 4096, extension: ".xvc");

        const string productId = "PRODUPLOAD";
        const string packageId = "pkg-1";

        host.Ingestion.StubGetProduct(productId);
        host.Ingestion.StubGetPackageBranches(productId, ("Main", "draft-1"));
        host.Ingestion.StubPackageConfiguration(productId, "draft-1", "config-1", marketGroupId: "NA");
        host.Ingestion.StubCreatePackage(productId, packageId, xfusUploadDomain: host.XfusUploadDomain);
        host.Ingestion.StubProcessPackage(productId, packageId, "Processed");
        host.Ingestion.StubGetPackageProcessing(productId, packageId, "Processed");
        host.Xfus.StubNoDeltaUploadSuccess(1024);

        var product = await host.Service.GetProductByProductIdAsync(productId, CancellationToken.None);
        var branch = await host.Service.GetPackageBranchByFriendlyNameAsync(product, "Main", CancellationToken.None);
        var config = await host.Service.GetPackageConfigurationAsync(product, branch, CancellationToken.None);
        var marketGroupPackage = config.MarketGroupPackages[0];

        var result = await host.Service.UploadGamePackageAsync(
            product,
            branch,
            marketGroupPackage,
            packageFile.Path,
            gameAssets: null,
            minutesToWaitForProcessing: 1,
            deltaUpload: false,
            isXvc: false,
            CancellationToken.None);

        Assert.IsNotNull(result);
        Assert.AreEqual(GamePackageState.Processed, result.State);

        // The file was actually uploaded to the XFUS fake: a block payload PUT must have occurred.
        Assert.IsTrue(
            host.Xfus.Server.LogEntries.Any(e =>
                e.RequestMessage!.Method == "PUT" &&
                e.RequestMessage.Path.Contains("/source/payload")),
            "XFUS fake should have received a block payload PUT");
    }
}
