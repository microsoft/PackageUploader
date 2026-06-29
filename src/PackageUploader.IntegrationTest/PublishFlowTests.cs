// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using PackageUploader.ClientApi.Client.Ingestion.Models;
using PackageUploader.IntegrationTest.Infrastructure;
using System.Threading;

namespace PackageUploader.IntegrationTest;

/// <summary>
/// End-to-end publish flow that exercises submission creation and polling against the Ingestion
/// fake: create a sandbox submission, then poll it until it reaches the Published state.
/// </summary>
[TestClass]
public sealed class PublishFlowTests : IntegrationTestBase
{
    [TestMethod]
    public async Task PublishToSandbox_PollsSubmission_UntilPublished()
    {
        using var host = CreateMockServerHost();

        const string productId = "PRODPUBLISH";
        const string submissionId = "sub-1";

        host.Ingestion.StubGetProduct(productId);
        host.Ingestion.StubGetPackageBranches(productId, ("Main", "draft-1"));
        host.Ingestion.StubCreateSubmission(productId, submissionId);
        host.Ingestion.StubGetSubmission(productId, submissionId, ("Published", "InStore"));

        var product = await host.Service.GetProductByProductIdAsync(productId, CancellationToken.None);
        var branch = await host.Service.GetPackageBranchByFriendlyNameAsync(product, "Main", CancellationToken.None);

        var submission = await host.Service.PublishPackagesToSandboxAsync(
            product, branch, "Sandbox.1", minutesToWaitForPublishing: 1, CancellationToken.None);

        Assert.IsNotNull(submission);
        Assert.AreEqual(GameSubmissionState.Published, submission.GameSubmissionState);
    }
}
