// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Mvc;

namespace PackageUploader.IntegrationTest.FakeApi;

/// <summary>
/// Fake XFUS upload service. Presents the <c>api/v2/assets/...</c> route space the client calls
/// (initialize, block payload PUT, continue) and delegates each request to the configured
/// <see cref="XfusScenarioStore"/>.
/// </summary>
public sealed class XfusController(XfusScenarioStore store) : FakeApiControllerBase
{
    [AcceptVerbs("GET", "POST", "PUT", Route = "api/v2/assets/{**rest}")]
    public Task<IActionResult> Handle() => RespondAsync(store);
}
