// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// CI re-run trigger placeholder.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using PackageUploader.IntegrationTest.Infrastructure;
using System.Linq;

namespace PackageUploader.IntegrationTest;

/// <summary>
/// Smoke test that validates the integration project is discovered, builds, and that the mock-server
/// host routes a public service call over real HTTP to the WireMock Ingestion fake with the fake
/// auth token attached.
/// </summary>
[TestClass]
public sealed class SmokeTest : IntegrationTestBase
{
    [TestMethod]
    public async Task TestHost_RoutesProductLookup_ThroughWireMockWithFakeAuth()
    {
        using var host = CreateMockServerHost();
        host.Ingestion.StubGetProduct("smoke-test-product");

        var product = await host.Service.GetProductByProductIdAsync("smoke-test-product", TestContext.CancellationToken);

        Assert.IsNotNull(product);
        Assert.AreEqual("smoke-test-product", product.ProductId);

        var logs = host.Ingestion.Server.LogEntries.ToList();
        Assert.AreEqual(1, logs.Count);

        var headers = logs[0].RequestMessage!.Headers!;
        Assert.IsTrue(headers.ContainsKey("Authorization"));
        StringAssert.Contains(string.Join(" ", headers["Authorization"]!), FakeAccessTokenProvider.FakeToken);
    }

    public TestContext TestContext { get; set; } = null!;
}
