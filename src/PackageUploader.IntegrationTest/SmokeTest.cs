// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using PackageUploader.IntegrationTest.Infrastructure;

namespace PackageUploader.IntegrationTest;

/// <summary>
/// Smoke test that validates the integration project is discovered, builds, and that the mock-server
/// host routes a public service call over real HTTP to the fake Ingestion API with the fake auth
/// token attached.
/// </summary>
[TestClass]
public sealed class SmokeTest : IntegrationTestBase
{
    [TestMethod]
    public async Task TestHost_RoutesProductLookup_ThroughFakeApiWithFakeAuth()
    {
        using var host = CreateMockServerHost();
        host.Ingestion.StubGetProduct("smoke-test-product");

        var product = await host.Service.GetProductByProductIdAsync("smoke-test-product", TestContext.CancellationToken);

        Assert.IsNotNull(product);
        Assert.AreEqual("smoke-test-product", product.ProductId);

        var requests = host.Ingestion.ReceivedRequests;
        Assert.AreEqual(1, requests.Count);

        var headers = requests[0].Headers;
        Assert.IsTrue(headers.ContainsKey("Authorization"));
        StringAssert.Contains(headers["Authorization"], FakeAccessTokenProvider.FakeToken);
    }

    public TestContext TestContext { get; set; } = null!;
}
