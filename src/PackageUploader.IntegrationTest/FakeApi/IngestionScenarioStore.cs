// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;
using System.Net.Http;
using PackageUploader.IntegrationTest.Infrastructure.Mocks;

namespace PackageUploader.IntegrationTest.FakeApi;

/// <summary>
/// Configurable scripted responses for the fake Ingestion API, consumed by <see cref="IngestionController"/>.
/// Exposes the same fluent stub surface as the other Task 2 prototypes.
/// </summary>
public sealed class IngestionScenarioStore : ScenarioStore
{
    public IngestionScenarioStore StubGetProduct(string productId, ResponseScenario scenario = ResponseScenario.Success)
    {
        if (scenario != ResponseScenario.Success)
        {
            On(HttpMethod.Get, $"/products/{productId}", () => new FakeResponse((int)StatusFor(scenario)));
            return this;
        }

        On(HttpMethod.Get, $"/products/{productId}", () => new FakeResponse(200, new
        {
            resourceType = "AzureGameProduct",
            name = $"Test Product {productId}",
            id = productId,
            externalIds = new[] { new { type = "StoreId", value = "9TESTBIGID000" } },
            isModularPublishing = true,
        }));
        return this;
    }

    public IngestionScenarioStore StubGetPackageBranches(
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

        On(HttpMethod.Get, $"/products/{productId}/branches/getByModule*", () => new FakeResponse(200, new { value = values }));
        On(HttpMethod.Get, $"/products/{productId}/flights", () => new FakeResponse(200, new { value = Array.Empty<object>() }));
        return this;
    }

    public IngestionScenarioStore StubCreatePackage(
        string productId,
        string packageId,
        string fileName = "test.xvc",
        string? xfusUploadDomain = null,
        string? xfusId = null,
        string xfusTenant = "DCE",
        string xfusToken = "fake-xfus-token")
    {
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

        On(HttpMethod.Post, $"/products/{productId}/packages", () => new FakeResponse(200, new
        {
            resourceType = "GamePackage",
            id = packageId,
            state = "PendingUpload",
            fileName,
            uploadInfo,
        }));
        return this;
    }

    public IngestionScenarioStore StubGetPackageProcessing(string productId, string packageId, params string[] stateProgression)
    {
        var states = stateProgression.Length > 0 ? stateProgression : ["Processed"];
        var responders = states.Select(state => (Func<FakeResponse>)(() => new FakeResponse(200, new
        {
            resourceType = "GamePackage",
            id = packageId,
            state,
        }))).ToArray();

        OnSequence(HttpMethod.Get, $"/products/{productId}/packages/{packageId}", responders);
        return this;
    }

    public IngestionScenarioStore StubProcessPackage(string productId, string packageId, string state = "Processed")
    {
        // Note: WaitForPackageProcessingAsync returns this PUT result, so this 'state' is the package
        // state surfaced by UploadGamePackageAsync (the GET poll state only governs loop timing).
        On(HttpMethod.Put, $"/products/{productId}/packages/{packageId}", () => new FakeResponse(200, new
        {
            resourceType = "GamePackage",
            id = packageId,
            state,
        }));
        return this;
    }

    public IngestionScenarioStore StubPackageConfiguration(
        string productId,
        string instanceId,
        string configId,
        string marketGroupId = "default",
        string marketGroupName = "default")
    {
        var marketGroupPackages = new[]
        {
            new { marketGroupId, name = marketGroupName, packageIds = Array.Empty<string>() },
        };

        // Include marketGroupPackages so the service treats the config as already-initialized (the
        // common path) rather than entering first-time initialization.
        On(HttpMethod.Get, $"/products/{productId}/packageConfigurations/getByInstanceID*", () => new FakeResponse(200, new
        {
            value = new[] { new { resourceType = "PackageConfiguration", id = configId, marketGroupPackages } },
        }));

        Func<FakeResponse> single = () => new FakeResponse(200, new
        {
            resourceType = "PackageConfiguration",
            id = configId,
            marketGroupPackages,
        });
        On(HttpMethod.Get, $"/products/{productId}/packageConfigurations/{configId}", single);
        On(HttpMethod.Put, $"/products/{productId}/packageConfigurations/{configId}", single);
        return this;
    }

    public IngestionScenarioStore StubCreateSubmission(string productId, string submissionId)
    {
        On(HttpMethod.Post, $"/products/{productId}/submissions", () => new FakeResponse(200, SubmissionBody(submissionId, "InProgress", "Submitted")));
        return this;
    }

    public IngestionScenarioStore StubGetSubmission(
        string productId,
        string submissionId,
        params (string State, string Substate)[] progression)
    {
        var steps = progression.Length > 0 ? progression : [("Published", "InStore")];
        var responders = steps.Select(step => (Func<FakeResponse>)(() =>
            new FakeResponse(200, SubmissionBody(submissionId, step.State, step.Substate)))).ToArray();

        OnSequence(HttpMethod.Get, $"/products/{productId}/submissions/{submissionId}", responders);
        return this;
    }

    public IngestionScenarioStore StubError(string method, string pathPattern, HttpStatusCode statusCode)
    {
        On(HttpMethod.Parse(method), pathPattern, () => new FakeResponse((int)statusCode));
        return this;
    }

    public IngestionScenarioStore StubRetryThenSuccess(
        string method,
        string path,
        object successBody,
        int failures = 2,
        HttpStatusCode failureStatus = HttpStatusCode.InternalServerError)
    {
        var responders = new List<Func<FakeResponse>>();
        for (var i = 0; i < failures; i++)
        {
            responders.Add(() => new FakeResponse((int)failureStatus));
        }
        responders.Add(() => new FakeResponse(200, successBody));

        OnSequence(HttpMethod.Parse(method), path, responders);
        return this;
    }

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
