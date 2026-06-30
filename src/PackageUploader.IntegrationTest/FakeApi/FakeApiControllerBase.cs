// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using PackageUploader.IntegrationTest.FakeApi;

namespace PackageUploader.IntegrationTest.FakeApi;

/// <summary>
/// Base for the fake-API controllers: records the incoming request into the given store and returns
/// the scripted response (status code, with an optional JSON body).
/// </summary>
public abstract class FakeApiControllerBase : ControllerBase
{
    private protected async Task<IActionResult> RespondAsync(ScenarioStore store)
    {
        string? body = null;
        if (Request.ContentLength is > 0)
        {
            using var reader = new StreamReader(Request.Body, leaveOpen: true);
            body = await reader.ReadToEndAsync();
        }

        var headers = Request.Headers.ToDictionary(
            h => h.Key,
            h => string.Join(", ", h.Value.ToArray()),
            StringComparer.OrdinalIgnoreCase);

        var path = Request.Path.Value ?? string.Empty;
        store.Record(Request.Method, path, headers, body);

        var response = store.Resolve(Request.Method, path);
        if (response.Body is null)
        {
            return StatusCode(response.StatusCode);
        }

        return new ContentResult
        {
            StatusCode = response.StatusCode,
            ContentType = "application/json",
            Content = JsonSerializer.Serialize(response.Body),
        };
    }
}
