// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using PackageUploader.IntegrationTest.Infrastructure;
using System.Net.Http;

namespace PackageUploader.IntegrationTest;

/// <summary>
/// Smoke test that validates the integration project is discovered, builds, and that the mock
/// harness routes a public service call through the real pipeline to the mock handler.
/// </summary>
[TestClass]
public sealed class SmokeTest : IntegrationTestBase
{
    [TestMethod]
    public async Task TestHost_RoutesProductLookup_ThroughMockHandlerWithFakeAuth()
    {
        using var host = CreateHost(mock =>
            mock.WhenJson(HttpMethod.Get, "/products/", "{\"id\":\"smoke-test-product\"}"));

        var product = await host.Service.GetProductByProductIdAsync("smoke-test-product", TestContext.CancellationToken);

        Assert.IsNotNull(product);
        Assert.AreEqual(1, host.IngestionHandler.ReceivedRequests.Count);

        var request = host.IngestionHandler.ReceivedRequests[0];
        Assert.IsTrue(request.Headers.ContainsKey("Authorization"));
        StringAssert.Contains(request.Headers["Authorization"], FakeAccessTokenProvider.FakeToken);
    }

    public TestContext TestContext { get; set; } = null!;
}
