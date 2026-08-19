// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using PackageUploader.ClientApi.Client.Ingestion.Exceptions;
using PackageUploader.IntegrationTest.Infrastructure;
using PackageUploader.IntegrationTest.Infrastructure.Mocks;
using System.Net;
using System.Threading;

namespace PackageUploader.IntegrationTest;

/// <summary>
/// End-to-end integration tests for Ingestion API flows, exercising the real service against the
/// WireMock Ingestion fake: success, error mapping, transient-error retry, and paged collections.
/// </summary>
[TestClass]
public sealed class IngestionApiTests : IntegrationTestBase
{
    [TestMethod]
    public async Task GetProductByProductId_Success_ReturnsMappedProduct()
    {
        using var host = CreateMockServerHost();
        host.Ingestion.StubGetProduct("9P000TEST");

        var product = await host.Service.GetProductByProductIdAsync("9P000TEST", CancellationToken.None);

        Assert.IsNotNull(product);
        Assert.AreEqual("9P000TEST", product.ProductId);
        Assert.AreEqual("Test Product 9P000TEST", product.ProductName);
    }

    [TestMethod]
    public async Task GetProductByProductId_NotFound_ThrowsProductNotFound()
    {
        using var host = CreateMockServerHost();
        host.Ingestion.StubGetProduct("MISSING", ResponseScenario.NotFound);

        await Assert.ThrowsExactlyAsync<ProductNotFoundException>(
            () => host.Service.GetProductByProductIdAsync("MISSING", CancellationToken.None));
    }

    [TestMethod]
    public async Task GetProductByProductId_RetriesTransientError_ThenSucceeds()
    {
        using var host = CreateMockServerHost();
        host.Ingestion.StubRetryThenSuccess(
            "GET",
            "/products/RETRYME",
            new { resourceType = "AzureGameProduct", id = "RETRYME", name = "Recovered" },
            failures: 2,
            failureStatus: HttpStatusCode.InternalServerError);

        var product = await host.Service.GetProductByProductIdAsync("RETRYME", CancellationToken.None);

        Assert.IsNotNull(product);
        Assert.AreEqual("RETRYME", product.ProductId);
    }

    [TestMethod]
    public async Task GetPackageBranches_ReturnsConfiguredBranches()
    {
        using var host = CreateMockServerHost();
        host.Ingestion.StubGetProduct("PRODX");
        host.Ingestion.StubGetPackageBranches("PRODX", ("Main", "draft-1"), ("Beta", "draft-2"));

        var product = await host.Service.GetProductByProductIdAsync("PRODX", CancellationToken.None);
        var branches = await host.Service.GetPackageBranchesAsync(product, CancellationToken.None);

        Assert.AreEqual(2, branches.Count);
        CollectionAssert.AreEquivalent(
            new[] { "Main", "Beta" },
            branches.Select(b => b.Name).ToArray());
    }
}
