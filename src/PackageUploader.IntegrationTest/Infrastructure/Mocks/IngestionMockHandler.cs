// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;
using System.Net.Http;

namespace PackageUploader.IntegrationTest.Infrastructure.Mocks;

/// <summary>
/// First-party (BCL-only) in-memory fake of the Partner Center Ingestion API, plugged in as the
/// primary handler of the Ingestion <see cref="HttpClient"/>. Exposes fluent stubs for the endpoints
/// PackageUploader uses, with configurable success / error / retry / polling scenarios.
/// </summary>
internal sealed class IngestionMockHandler : StubHttpMessageHandler
{
    // ---- GetProduct: GET /products/{id} ----

    public IngestionMockHandler StubGetProduct(string productId, ResponseScenario scenario = ResponseScenario.Success)
    {
        if (scenario != ResponseScenario.Success)
        {
            On(HttpMethod.Get, $"/products/{productId}", () => Status(StatusFor(scenario)));
            return this;
        }

        On(HttpMethod.Get, $"/products/{productId}", () => Json(new
        {
            resourceType = "AzureGameProduct",
            name = $"Test Product {productId}",
            id = productId,
            externalIds = new[] { new { type = "StoreId", value = "9TESTBIGID000" } },
            isModularPublishing = true,
        }));
        return this;
    }

    // ---- GetBranches: GET /products/{id}/branches/getByModule(module=Package) (+ flights) ----

    public IngestionMockHandler StubGetPackageBranches(
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
        }).ToArray();

        On(HttpMethod.Get, $"/products/{productId}/branches/getByModule*", () => Json(new { value = values }));
        // GetPackageBranchesAsync enumerates flights first; stub them as empty.
        On(HttpMethod.Get, $"/products/{productId}/flights", () => Json(new { value = Array.Empty<object>() }));
        return this;
    }

    // ---- CreatePackageRequest: POST /products/{id}/packages ----

    public IngestionMockHandler StubCreatePackage(
        string productId,
        string packageId,
        string fileName = "test.xvc",
        string? xfusUploadDomain = null,
        string? xfusId = null,
        string xfusTenant = "DCE",
        string xfusToken = "fake-xfus-token")
    {
        // Use null (not an empty object) for absent upload info so the field is omitted entirely; an
        // empty object would deserialize into a non-null upload info with a null XfusId and crash the
        // client's Guid mapping.
        object? uploadInfo = xfusUploadDomain is null
            ? null
            : new
            {
                fileName,
                xfusId = xfusId ?? Guid.NewGuid().ToString(),
                token = xfusToken,
                uploadDomain = xfusUploadDomain,
                xfusTenant,
            };

        On(HttpMethod.Post, $"/products/{productId}/packages", () => Json(new
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

    public IngestionMockHandler StubGetPackageProcessing(string productId, string packageId, params string[] stateProgression)
    {
        var states = stateProgression.Length > 0 ? stateProgression : ["Processed"];
        var responders = states.Select(state => (Func<HttpResponseMessage>)(() => Json(new
        {
            resourceType = "GamePackage",
            id = packageId,
            state,
        }))).ToArray();

        OnSequence(HttpMethod.Get, $"/products/{productId}/packages/{packageId}", responders);
        return this;
    }

    // ---- ProcessPackage: PUT /products/{id}/packages/{packageId} ----

    public IngestionMockHandler StubProcessPackage(string productId, string packageId, string state = "Uploaded")
    {
        On(HttpMethod.Put, $"/products/{productId}/packages/{packageId}", () => Json(new
        {
            resourceType = "GamePackage",
            id = packageId,
            state,
        }));
        return this;
    }

    // ---- GetPackageConfig ----

    public IngestionMockHandler StubPackageConfiguration(
        string productId,
        string instanceId,
        string configId,
        string marketGroupId = "default",
        string marketGroupName = "default")
    {
        On(HttpMethod.Get, $"/products/{productId}/packageConfigurations/getByInstanceID*", () => Json(new
        {
            value = new[] { new { resourceType = "PackageConfiguration", id = configId } },
        }));

        Func<HttpResponseMessage> single = () => Json(new
        {
            resourceType = "PackageConfiguration",
            id = configId,
            marketGroupPackages = new[]
            {
                new { marketGroupId, name = marketGroupName, packageIds = Array.Empty<string>() },
            },
        });
        On(HttpMethod.Get, $"/products/{productId}/packageConfigurations/{configId}", single);
        On(HttpMethod.Put, $"/products/{productId}/packageConfigurations/{configId}", single);
        return this;
    }

    // ---- CreateSubmission / GetSubmission ----

    public IngestionMockHandler StubCreateSubmission(string productId, string submissionId)
    {
        On(HttpMethod.Post, $"/products/{productId}/submissions", () => Json(SubmissionBody(submissionId, "InProgress", "Submitted")));
        return this;
    }

    public IngestionMockHandler StubGetSubmission(
        string productId,
        string submissionId,
        params (string State, string Substate)[] progression)
    {
        var steps = progression.Length > 0 ? progression : [("Published", "InStore")];
        var responders = steps.Select(step => (Func<HttpResponseMessage>)(() =>
            Json(SubmissionBody(submissionId, step.State, step.Substate)))).ToArray();

        OnSequence(HttpMethod.Get, $"/products/{productId}/submissions/{submissionId}", responders);
        return this;
    }

    // ---- Generic scenario primitives ----

    public IngestionMockHandler StubError(string method, string pathPattern, HttpStatusCode statusCode)
    {
        On(HttpMethod.Parse(method), pathPattern, () => Status(statusCode));
        return this;
    }

    public IngestionMockHandler StubRetryThenSuccess(
        string method,
        string path,
        object successBody,
        int failures = 2,
        HttpStatusCode failureStatus = HttpStatusCode.InternalServerError)
    {
        var responders = new List<Func<HttpResponseMessage>>();
        for (var i = 0; i < failures; i++)
        {
            responders.Add(() => Status(failureStatus));
        }
        responders.Add(() => Json(successBody));

        OnSequence(HttpMethod.Parse(method), path, responders);
        return this;
    }

    // ---- helpers ----

    private static object SubmissionBody(string submissionId, string state, string substate) => new
    {
        resourceType = "Submission",
        id = submissionId,
        state,
        substate,
        // PendingUpdateInfo.Status is dereferenced by the submission-state mapper, so it must be present.
        pendingUpdateInfo = new { status = "Completed" },
    };

    private static HttpStatusCode StatusFor(ResponseScenario scenario) => scenario switch
    {
        ResponseScenario.ServerError => HttpStatusCode.InternalServerError,
        ResponseScenario.Unauthorized => HttpStatusCode.Unauthorized,
        ResponseScenario.NotFound => HttpStatusCode.NotFound,
        _ => HttpStatusCode.InternalServerError,
    };
}
