// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Mvc;

namespace PackageUploader.IntegrationTest.FakeApi;

/// <summary>
/// Fake Partner Center Ingestion API. Presents the <c>products/...</c> route space the client calls
/// and delegates each request to the configured <see cref="IngestionScenarioStore"/>.
/// </summary>
public sealed class IngestionController(IngestionScenarioStore store) : FakeApiControllerBase
{
    [AcceptVerbs("GET", "POST", "PUT", Route = "products/{**rest}")]
    public Task<IActionResult> Handle() => RespondAsync(store);
}
