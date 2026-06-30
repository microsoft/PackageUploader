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
            // Only decode textual bodies; binary payloads (e.g. XFUS octet-stream block uploads)
            // would be corrupted by a UTF-8 text read, so record their size instead.
            var contentType = Request.ContentType ?? string.Empty;
            if (contentType.Contains("json", StringComparison.OrdinalIgnoreCase) ||
                contentType.StartsWith("text/", StringComparison.OrdinalIgnoreCase))
            {
                using var reader = new StreamReader(Request.Body, leaveOpen: true);
                body = await reader.ReadToEndAsync();
            }
            else
            {
                body = $"<{Request.ContentLength} binary bytes>";
            }
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
