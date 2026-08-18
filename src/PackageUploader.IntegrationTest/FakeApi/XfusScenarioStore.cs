// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;
using System.Net.Http;

namespace PackageUploader.IntegrationTest.FakeApi;

/// <summary>
/// Configurable scripted responses for the fake XFUS upload service, consumed by <see cref="XfusController"/>.
/// Serves the three-step chunked upload (initialize -> block payload PUT -> continue) for the
/// no-delta path, with configurable success / error / retry scenarios.
/// </summary>
/// <remarks>
/// Responses omit <c>directUploadParameters.sasUri</c> so the client uploads blocks via the proxy
/// PUT path. The <c>status</c> field is emitted as a number (ReceivingBlocks=0, Busy=1, Completed=2).
/// </remarks>
public sealed class XfusScenarioStore : ScenarioStore
{
    private const string AssetsRoot = "/api/v2/assets";

    public XfusScenarioStore StubNoDeltaUploadSuccess(params long[] blockSizes)
    {
        var sizes = blockSizes.Length > 0 ? blockSizes : [64L * 1024];
        StubInitialize(UploadProgress(sizes, XfusStatus.ReceivingBlocks));
        StubBlockUpload();
        StubContinue(UploadProgress([], XfusStatus.Completed));
        return this;
    }

    public XfusScenarioStore StubInitialize(object uploadProgressBody)
    {
        On(HttpMethod.Post, $"{AssetsRoot}/*/initialize", () => new FakeResponse(200, uploadProgressBody));
        return this;
    }

    public XfusScenarioStore StubBlockUpload(HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        On(HttpMethod.Put, $"{AssetsRoot}/*/blocks/*/source/payload", () => new FakeResponse((int)statusCode));
        return this;
    }

    public XfusScenarioStore StubContinue(object uploadProgressBody)
    {
        On(HttpMethod.Post, $"{AssetsRoot}/*/continue", () => new FakeResponse(200, uploadProgressBody));
        return this;
    }

    public XfusScenarioStore StubContinueProgression(params object[] uploadProgressBodies)
    {
        var bodies = uploadProgressBodies.Length > 0 ? uploadProgressBodies : [UploadProgress([], XfusStatus.Completed)];
        var responders = bodies.Select(b => (Func<FakeResponse>)(() => new FakeResponse(200, b))).ToArray();
        OnSequence(HttpMethod.Post, $"{AssetsRoot}/*/continue", responders);
        return this;
    }

    public XfusScenarioStore StubError(string method, string pathPattern, HttpStatusCode statusCode)
    {
        On(HttpMethod.Parse(method), pathPattern, () => new FakeResponse((int)statusCode));
        return this;
    }

    public XfusScenarioStore StubBlockUploadRetryThenSuccess(int failures = 1, HttpStatusCode failureStatus = HttpStatusCode.ServiceUnavailable)
    {
        var responders = new List<Func<FakeResponse>>();
        for (var i = 0; i < failures; i++)
        {
            responders.Add(() => new FakeResponse((int)failureStatus));
        }
        responders.Add(() => new FakeResponse((int)HttpStatusCode.OK));

        OnSequence(HttpMethod.Put, $"{AssetsRoot}/*/blocks/*/source/payload", responders);
        return this;
    }

    private enum XfusStatus
    {
        ReceivingBlocks = 0,
        Busy = 1,
        Completed = 2,
    }

    private static object UploadProgress(long[] blockSizes, XfusStatus status)
    {
        long offset = 0;
        var blocks = new List<object>();
        for (long i = 0; i < blockSizes.Length; i++)
        {
            blocks.Add(new
            {
                id = i,
                blockIdBase64 = Convert.ToBase64String(BitConverter.GetBytes(i)),
                offset,
                size = blockSizes[i],
            });
            offset += blockSizes[i];
        }

        return new
        {
            pendingBlocks = blocks,
            status = (int)status,
        };
    }
}
