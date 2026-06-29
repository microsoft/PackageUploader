// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace PackageUploader.IntegrationTest.Infrastructure.Mocks;

/// <summary>
/// A reusable WireMock.Net-backed fake of the Partner Center Ingestion API. Starts a real HTTP
/// server on a random local port and exposes fluent stubs for the endpoints PackageUploader uses,
/// with configurable success / error / retry / polling scenarios. Dispose to stop the server.
/// </summary>
internal sealed class IngestionMockServer : IDisposable
{
    private readonly WireMockServer _server;
    private readonly string _id = Guid.NewGuid().ToString("N");

    public IngestionMockServer() => _server = WireMockServer.Start();

    /// <summary>Base URL of the running mock server (e.g. http://localhost:12345).</summary>
    public string Url => _server.Url!;

    /// <summary>The underlying server, for advanced stubbing or request-log assertions.</summary>
    public WireMockServer Server => _server;

    // ---- GetProduct: GET /products/{id} ----

    public IngestionMockServer StubGetProduct(string productId, ResponseScenario scenario = ResponseScenario.Success)
    {
        var request = Request.Create().WithPath($"/products/{productId}").UsingGet();
        if (scenario != ResponseScenario.Success)
        {
            _server.Given(request).RespondWith(ErrorResponse(scenario));
            return this;
        }

        _server.Given(request).RespondWith(JsonResponse(new
        {
            resourceType = "AzureGameProduct",
            name = $"Test Product {productId}",
            id = productId,
            externalIds = new[] { new { type = "StoreId", value = "9TESTBIGID000" } },
            isModularPublishing = true,
        }));
        return this;
    }

    // ---- GetBranches: GET /products/{id}/branches/getByModule(module=Package) ----

    public IngestionMockServer StubGetPackageBranches(
        string productId,
        params (string FriendlyName, string CurrentDraftInstanceId)[] branches)
    {
        var values = branches.Select(b => new
        {
            resourceType = "Branch",
            friendlyName = b.FriendlyName,
            type = "Main",
            module = "Package",
            currentDraftInstanceId = b.CurrentDraftInstanceId,
        });

        _server
            .Given(Request.Create().WithPath("/products/" + productId + "/branches/getByModule*").UsingGet())
            .RespondWith(JsonResponse(new { value = values }));
        return this;
    }

    // ---- CreatePackageRequest: POST /products/{id}/packages ----

    public IngestionMockServer StubCreatePackage(
        string productId,
        string packageId,
        string fileName = "test.xvc",
        string? xfusUploadDomain = null,
        string? xfusId = null,
        string xfusTenant = "DCE",
        string xfusToken = "fake-xfus-token")
    {
        // When an XFUS upload domain is supplied, embed the upload info so the client uploads to the
        // XFUS mock; xfusId must be a valid GUID because the client maps it to a Guid.
        object uploadInfo = xfusUploadDomain is null
            ? new { }
            : new
            {
                fileName,
                xfusId = xfusId ?? Guid.NewGuid().ToString(),
                token = xfusToken,
                uploadDomain = xfusUploadDomain,
                xfusTenant,
            };

        _server
            .Given(Request.Create().WithPath($"/products/{productId}/packages").UsingPost())
            .RespondWith(JsonResponse(new
            {
                resourceType = "GamePackage",
                id = packageId,
                state = "PendingUpload",
                fileName,
                uploadInfo,
            }));
        return this;
    }

    // ---- GetPackage processing poll: GET /products/{id}/packages/{packageId} ----
    // Returns each state in order across successive calls, then stays on the final state.

    public IngestionMockServer StubGetPackageProcessing(
        string productId,
        string packageId,
        params string[] stateProgression)
    {
        var states = stateProgression.Length > 0 ? stateProgression : ["Processed"];
        var request = Request.Create().WithPath($"/products/{productId}/packages/{packageId}").UsingGet();
        var scenario = $"package-{productId}-{packageId}-{_id}";

        for (int i = 0; i < states.Length; i++)
        {
            bool isLast = i == states.Length - 1;
            var body = JsonResponse(new
            {
                resourceType = "GamePackage",
                id = packageId,
                state = states[i],
            });

            var builder = _server.Given(request).InScenario(scenario);
            if (i > 0)
            {
                builder = builder.WhenStateIs(StateName(i));
            }
            if (!isLast)
            {
                builder = builder.WillSetStateTo(StateName(i + 1));
            }
            else if (i > 0)
            {
                builder = builder.WillSetStateTo(StateName(i));
            }

            builder.RespondWith(body);
        }

        return this;
    }

    // ---- GetPackageConfig: GET /products/{id}/packageConfigurations/getByInstanceID(...) and GET/PUT by id ----

    public IngestionMockServer StubPackageConfiguration(string productId, string instanceId, string configId)
    {
        var body = JsonResponse(new
        {
            value = new[] { new { resourceType = "PackageConfiguration", id = configId } },
        });

        _server
            .Given(Request.Create().WithPath("/products/" + productId + "/packageConfigurations/getByInstanceID*").UsingGet())
            .RespondWith(body);

        var single = JsonResponse(new { resourceType = "PackageConfiguration", id = configId });
        _server
            .Given(Request.Create().WithPath($"/products/{productId}/packageConfigurations/{configId}").UsingGet())
            .RespondWith(single);
        _server
            .Given(Request.Create().WithPath($"/products/{productId}/packageConfigurations/{configId}").UsingPut())
            .RespondWith(single);
        return this;
    }

    // ---- CreateSubmission: POST /products/{id}/submissions ----

    public IngestionMockServer StubCreateSubmission(string productId, string submissionId)
    {
        _server
            .Given(Request.Create().WithPath($"/products/{productId}/submissions").UsingPost())
            .RespondWith(JsonResponse(new
            {
                resourceType = "Submission",
                id = submissionId,
                state = "InProgress",
                substate = "Submitted",
            }));
        return this;
    }

    // ---- GetSubmission poll: GET /products/{id}/submissions/{id} ----
    // Returns each (state, substate) in order across successive calls, then stays on the final one.

    public IngestionMockServer StubGetSubmission(
        string productId,
        string submissionId,
        params (string State, string Substate)[] progression)
    {
        var steps = progression.Length > 0 ? progression : [("Published", "InStore")];
        var request = Request.Create().WithPath($"/products/{productId}/submissions/{submissionId}").UsingGet();
        var scenario = $"submission-{productId}-{submissionId}-{_id}";

        for (int i = 0; i < steps.Length; i++)
        {
            bool isLast = i == steps.Length - 1;
            var body = JsonResponse(new
            {
                resourceType = "Submission",
                id = submissionId,
                state = steps[i].State,
                substate = steps[i].Substate,
            });

            var builder = _server.Given(request).InScenario(scenario);
            if (i > 0)
            {
                builder = builder.WhenStateIs(StateName(i));
            }
            if (!isLast)
            {
                builder = builder.WillSetStateTo(StateName(i + 1));
            }
            else if (i > 0)
            {
                builder = builder.WillSetStateTo(StateName(i));
            }

            builder.RespondWith(body);
        }

        return this;
    }

    // ---- Generic scenario primitives ----

    /// <summary>Always responds to the given method/path-wildcard with the supplied status code.</summary>
    public IngestionMockServer StubError(string method, string pathWildcard, HttpStatusCode statusCode)
    {
        _server
            .Given(Request.Create().WithPath(pathWildcard).UsingMethod(method))
            .RespondWith(Response.Create().WithStatusCode((int)statusCode));
        return this;
    }

    /// <summary>
    /// Fails the first <paramref name="failures"/> calls with <paramref name="failureStatus"/>, then
    /// succeeds with the given JSON body — used to exercise the client's Polly retry policy.
    /// </summary>
    public IngestionMockServer StubRetryThenSuccess(
        string method,
        string path,
        object successBody,
        int failures = 2,
        HttpStatusCode failureStatus = HttpStatusCode.InternalServerError)
    {
        var request = Request.Create().WithPath(path).UsingMethod(method);
        var scenario = $"retry-{method}-{path}-{_id}";

        for (int i = 0; i < failures; i++)
        {
            var builder = _server.Given(request).InScenario(scenario);
            if (i > 0)
            {
                builder = builder.WhenStateIs(StateName(i));
            }
            builder.WillSetStateTo(StateName(i + 1))
                .RespondWith(Response.Create().WithStatusCode((int)failureStatus));
        }

        _server.Given(request).InScenario(scenario)
            .WhenStateIs(StateName(failures))
            .RespondWith(JsonResponse(successBody));
        return this;
    }

    public void Dispose() => _server.Stop();

    // ---- helpers ----

    private static string StateName(int index) => $"step-{index}";

    private static IResponseBuilder JsonResponse(object body) =>
        Response.Create()
            .WithStatusCode(200)
            .WithHeader("Content-Type", "application/json")
            .WithBodyAsJson(body);

    private static IResponseBuilder ErrorResponse(ResponseScenario scenario) =>
        Response.Create().WithStatusCode((int)scenario switch
        {
            _ when scenario == ResponseScenario.ServerError => HttpStatusCode.InternalServerError,
            _ when scenario == ResponseScenario.Unauthorized => HttpStatusCode.Unauthorized,
            _ when scenario == ResponseScenario.NotFound => HttpStatusCode.NotFound,
            _ => HttpStatusCode.InternalServerError,
        });
}
