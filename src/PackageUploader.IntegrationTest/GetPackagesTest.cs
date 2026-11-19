// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using PackageUploader.Application;
using PackageUploader.IntegrationTest.Infrastructure;
using System.Net;
using System.Net.Http;
using System.Text.Json;

namespace PackageUploader.IntegrationTest;

[TestClass]
[DoNotParallelize]
public sealed class GetPackagesTest : IntegrationTestBase
{
    private const string PackageResponse = """
        {
          "id": "canonical-package",
          "state": "Processed",
          "fileName": "example.msixvc",
          "fileSize": 8589934592,
          "packageType": "Msixvc",
          "xvcTargetPlatform": "PC",
          "buildId": "11111111-2222-4333-8444-555555555555",
          "contentId": "aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee",
          "version": "1.0.0.0",
          "eTag": "private-etag",
          "@odata.etag": "private-odata-etag",
          "uploadInfo": {
            "xfusId": "12345678-1234-4234-8234-123456789012",
            "token": "synthetic-upload-token",
            "fileSasUri": "https://upload.example.test/package?sig=synthetic-signature",
            "uploadDomain": "https://upload.example.test",
            "xfusTenant": "synthetic-tenant"
          }
        }
        """;

    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task Data_ProjectsOnlyMetadata_FromSelectedDraftAndMarketGroup(bool flight)
    {
        var result = await RunAsync(["package-1"], mock =>
            mock.WhenJson(HttpMethod.Get, "/packages/package-1", PackageResponse), flight: flight);

        Assert.AreEqual(0, result.ExitCode);
        Assert.AreEqual(string.Empty, result.Stderr);
        using var document = JsonDocument.Parse(result.Stdout);
        Assert.AreEqual(JsonValueKind.Array, document.RootElement.ValueKind);
        Assert.AreEqual(1, document.RootElement.GetArrayLength());
        var package = document.RootElement[0];
        CollectionAssert.AreEquivalent(
            new[] { "fileName", "fileSize", "id", "state", "packageType", "xvcTargetPlatform", "buildId", "contentId", "version" },
            package.EnumerateObject().Select(property => property.Name).ToArray());
        Assert.AreEqual("example.msixvc", package.GetProperty("fileName").GetString());
        Assert.AreEqual(JsonValueKind.Number, package.GetProperty("fileSize").ValueKind);
        Assert.AreEqual(8589934592L, package.GetProperty("fileSize").GetInt64());
        Assert.AreEqual("canonical-package", package.GetProperty("id").GetString());
        Assert.AreEqual("Processed", package.GetProperty("state").GetString());
        Assert.AreEqual("Msixvc", package.GetProperty("packageType").GetString());
        Assert.AreEqual("PC", package.GetProperty("xvcTargetPlatform").GetString());
        Assert.AreEqual("11111111-2222-4333-8444-555555555555", package.GetProperty("buildId").GetString());
        Assert.AreEqual("aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee", package.GetProperty("contentId").GetString());
        Assert.AreEqual("1.0.0.0", package.GetProperty("version").GetString());
        Assert.IsFalse(result.Stdout.Contains("synthetic-", StringComparison.Ordinal));
        Assert.IsFalse(result.Stdout.Contains("private-", StringComparison.Ordinal));
        Assert.IsTrue(result.Requests.All(request => request.Method == HttpMethod.Get));
        Assert.IsTrue(result.Requests.Any(request =>
            request.Uri.AbsolutePath.EndsWith($"getByInstanceID(instanceID={(flight ? "flight-draft" : "branch-draft")})")));
        Assert.HasCount(1, result.Requests.Where(request => request.Uri.AbsolutePath.Contains("/packages/")));
        AssertPackageIdentityQueries(result.Requests);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow(", \"buildId\": null, \"contentId\": null, \"version\": null")]
    public async Task Data_OmitsUnavailableIdentity_WithoutDerivingItFromOtherFields(string identity)
    {
        var result = await RunAsync(["package-1"], mock =>
            mock.WhenJson(HttpMethod.Get, "/packages/package-1", $$"""
                {
                  "id": "11111111-2222-4333-8444-555555555555",
                  "uid": "aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee",
                  "state": "PendingUpload",
                  "fileName": "example_1.2.3.4.msixvc",
                  "fileSize": null{{identity}}
                }
                """));

        Assert.AreEqual(0, result.ExitCode);
        Assert.AreEqual(string.Empty, result.Stderr);
        using var document = JsonDocument.Parse(result.Stdout);
        var package = document.RootElement[0];
        CollectionAssert.AreEquivalent(new[] { "fileName", "id", "state" },
            package.EnumerateObject().Select(property => property.Name).ToArray());
        Assert.AreEqual("PendingUpload", package.GetProperty("state").GetString());
        Assert.HasCount(1, result.Requests.Where(request => request.Uri.AbsolutePath.Contains("/packages/")));
        AssertPackageIdentityQueries(result.Requests);
    }

    [TestMethod]
    public async Task Data_EmptyMarketGroup_WritesEmptyArray()
    {
        var result = await RunAsync([], _ => { });

        Assert.AreEqual(0, result.ExitCode);
        Assert.AreEqual("[]", result.Stdout.Trim());
        Assert.AreEqual(string.Empty, result.Stderr);
        Assert.IsFalse(result.Requests.Any(request => request.Uri.AbsolutePath.Contains("/packages/")));
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("FutureState")]
    [DataRow("999")]
    public async Task Data_UnknownState_RemainsMeaningfulText(string? state)
    {
        var result = await RunAsync(["package-1"], mock =>
            mock.WhenJson(HttpMethod.Get, "/packages/package-1", JsonSerializer.Serialize(new { id = "package-1", state })));

        Assert.AreEqual(0, result.ExitCode);
        using var document = JsonDocument.Parse(result.Stdout);
        Assert.AreEqual("Unknown", document.RootElement[0].GetProperty("state").GetString());
    }

    [TestMethod]
    [DataRow(HttpStatusCode.BadRequest, "{}")]
    [DataRow(HttpStatusCode.NotFound, "{}")]
    [DataRow(HttpStatusCode.Unauthorized, "{}")]
    [DataRow(HttpStatusCode.Forbidden, "{}")]
    [DataRow(HttpStatusCode.InternalServerError, "{}")]
    [DataRow(HttpStatusCode.OK, """{"buildId": {}}""")]
    public async Task Data_LaterLookupFails_ReturnsNonzeroWithoutPartialArray(HttpStatusCode status, string response)
    {
        var result = await RunAsync(["package-1", "package-2"], mock => mock
            .WhenJson(HttpMethod.Get, "/packages/package-1", PackageResponse)
            .WhenJson(HttpMethod.Get, "/packages/package-2", response, status));

        Assert.AreEqual(3, result.ExitCode);
        Assert.AreEqual(string.Empty, result.Stdout);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.Stderr));
        CollectionAssert.AreEqual(new[] { "/products/test-product/packages/package-1", "/products/test-product/packages/package-2" },
            result.Requests.Where(request => request.Uri.AbsolutePath.Contains("/packages/"))
                .Select(request => request.Uri.AbsolutePath).Distinct().ToArray());
        // Only the existing authentication handler retries 401/403; no capability fallback is allowed.
        Assert.HasCount(status is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden ? 3 : 2,
            result.Requests.Where(request => request.Uri.AbsolutePath.Contains("/packages/")));
        AssertPackageIdentityQueries(result.Requests);
    }

    [TestMethod]
    public async Task Data_MarketGroupNameIsCaseSensitive_ReturnsErrorWithoutArray()
    {
        var result = await RunAsync(["package-1"], _ => { }, marketGroup: "selected");

        Assert.AreEqual(3, result.ExitCode);
        Assert.AreEqual(string.Empty, result.Stdout);
        StringAssert.Contains(result.Stderr, "case sensitive");
        Assert.IsFalse(result.Requests.Any(request => request.Uri.AbsolutePath.Contains("/packages/")));
    }

    [TestMethod]
    public async Task Data_RedirectedPackage_UsesCanonicalResourceAndIdentity()
    {
        var result = await RunAsync(["original-package"], mock => mock
            .WhenJson(HttpMethod.Get, "/packages/original-package", """{"toId":"canonical-package"}""", HttpStatusCode.MovedPermanently)
            .WhenJson(HttpMethod.Get, "/packages/canonical-package", PackageResponse));

        Assert.AreEqual(0, result.ExitCode);
        using var document = JsonDocument.Parse(result.Stdout);
        var package = document.RootElement[0];
        Assert.AreEqual("canonical-package", package.GetProperty("id").GetString());
        Assert.AreEqual("11111111-2222-4333-8444-555555555555", package.GetProperty("buildId").GetString());
        CollectionAssert.AreEqual(new[] { "original-package", "original-package", "canonical-package" },
            result.Requests.Where(request => request.Uri.AbsolutePath.Contains("/packages/"))
                .Select(request => request.Uri.Segments[^1]).ToArray());
        AssertPackageIdentityQueries(result.Requests);
    }

    private static void AssertPackageIdentityQueries(IReadOnlyList<RecordedRequest> requests)
    {
        foreach (var request in requests)
        {
            Assert.AreEqual(request.Uri.AbsolutePath.Contains("/packages/") ? "?$expand=PackageIdentity" : string.Empty,
                request.Uri.Query, $"Unexpected query for {request.Uri.AbsolutePath}.");
        }
    }

    private async Task<CommandResult> RunAsync(string[] packageIds, Action<MockHttpMessageHandler> configure,
        bool flight = false, string marketGroup = "Selected")
    {
        var configPath = Path.GetTempFileName();
        var logPath = configPath + ".log";
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();
        var originalOut = Console.Out;
        var originalError = Console.Error;
        try
        {
            await File.WriteAllTextAsync(configPath, JsonSerializer.Serialize(new
            {
                operationName = "GetPackages",
                productId = "test-product",
                branchFriendlyName = flight ? null : "Main",
                flightName = flight ? "Preview" : null,
                marketGroupName = marketGroup,
                ingestionConfig = new { retryCount = 0 },
            }), TestContext.CancellationToken);
            var parseResult = CommandLineHelper.BuildRootCommand().Parse(
                ["GetPackages", "--ConfigFile", configPath, "--Data", "--Verbose", "--LogFile", logPath]);
            Assert.IsEmpty(parseResult.Errors);

            Console.SetOut(stdout);
            Console.SetError(stderr);
            int exitCode;
            IReadOnlyList<RecordedRequest> requests;
            using (var host = new PackageUploaderTestHost(mock =>
            {
                configure(mock);
                mock.WhenJson(HttpMethod.Get, "/branches/getByModule", """
                    {"value":[
                      {"friendlyName":"Main","currentDraftInstanceId":"branch-draft"},
                      {"friendlyName":"flight-resource","currentDraftInstanceId":"flight-draft"}
                    ]}
                    """);
                mock.WhenJson(HttpMethod.Get, "/flights", """{"value":[{"id":"flight-resource","name":"Preview"}]}""");
                mock.WhenJson(HttpMethod.Get, $"/packageConfigurations/getByInstanceID(instanceID={(flight ? "flight-draft" : "branch-draft")})",
                    JsonSerializer.Serialize(new
                    {
                        value = new[]
                        {
                            new
                            {
                                id = "selected-configuration",
                                marketGroupPackages = new[]
                                {
                                    new { name = "Unselected", packageIds = new[] { "do-not-fetch" } },
                                    new { name = "Selected", packageIds },
                                },
                            },
                        },
                    }));
                mock.WhenJson(HttpMethod.Get, "/products/test-product", """{"id":"test-product"}""");
            }, parseResult: parseResult))
            {
                exitCode = await host.RunGetPackagesAsync(TestContext.CancellationToken);
                requests = host.IngestionHandler.ReceivedRequests;
            }

            return new CommandResult(exitCode, stdout.ToString(), stderr.ToString(), requests);
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
            File.Delete(configPath);
            File.Delete(logPath);
        }
    }

    private sealed record CommandResult(int ExitCode, string Stdout, string Stderr, IReadOnlyList<RecordedRequest> Requests);
}
