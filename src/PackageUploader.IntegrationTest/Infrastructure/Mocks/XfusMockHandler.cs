// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;
using System.Net.Http;

namespace PackageUploader.IntegrationTest.Infrastructure.Mocks;

/// <summary>
/// First-party (BCL-only) in-memory fake of the XFUS upload service, plugged in as the primary
/// handler of the XFUS <see cref="HttpClient"/>. Serves the three-step chunked upload (initialize ->
/// block payload PUT -> continue) rooted at <c>/api/v2/assets/</c> for the no-delta path, with
/// configurable success / error / retry scenarios.
/// </summary>
/// <remarks>
/// Responses omit <c>directUploadParameters.sasUri</c> so the client uploads blocks via the proxy
/// PUT path. The <c>status</c> field is emitted as a number (ReceivingBlocks=0, Busy=1, Completed=2)
/// because the client's serializer has no string-enum converter.
/// </remarks>
internal sealed class XfusMockHandler : StubHttpMessageHandler
{
    private const string AssetsRoot = "/api/v2/assets";

    public XfusMockHandler StubNoDeltaUploadSuccess(params long[] blockSizes)
    {
        var sizes = blockSizes.Length > 0 ? blockSizes : [64L * 1024];
        StubInitialize(UploadProgress(sizes, XfusStatus.ReceivingBlocks));
        StubBlockUpload();
        StubContinue(UploadProgress([], XfusStatus.Completed));
        return this;
    }

    public XfusMockHandler StubInitialize(object uploadProgressBody)
    {
        On(HttpMethod.Post, $"{AssetsRoot}/*/initialize", () => Json(uploadProgressBody));
        return this;
    }

    public XfusMockHandler StubBlockUpload(HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        On(HttpMethod.Put, $"{AssetsRoot}/*/blocks/*/source/payload", () => Status(statusCode));
        return this;
    }

    public XfusMockHandler StubContinue(object uploadProgressBody)
    {
        On(HttpMethod.Post, $"{AssetsRoot}/*/continue", () => Json(uploadProgressBody));
        return this;
    }

    public XfusMockHandler StubContinueProgression(params object[] uploadProgressBodies)
    {
        var bodies = uploadProgressBodies.Length > 0 ? uploadProgressBodies : [UploadProgress([], XfusStatus.Completed)];
        var responders = bodies.Select(b => (Func<HttpResponseMessage>)(() => Json(b))).ToArray();
        OnSequence(HttpMethod.Post, $"{AssetsRoot}/*/continue", responders);
        return this;
    }

    public XfusMockHandler StubError(string method, string pathPattern, HttpStatusCode statusCode)
    {
        On(HttpMethod.Parse(method), pathPattern, () => Status(statusCode));
        return this;
    }

    public XfusMockHandler StubBlockUploadRetryThenSuccess(int failures = 1, HttpStatusCode failureStatus = HttpStatusCode.ServiceUnavailable)
    {
        var responders = new List<Func<HttpResponseMessage>>();
        for (var i = 0; i < failures; i++)
        {
            responders.Add(() => Status(failureStatus));
        }
        responders.Add(() => Status(HttpStatusCode.OK));

        OnSequence(HttpMethod.Put, $"{AssetsRoot}/*/blocks/*/source/payload", responders);
        return this;
    }

    // ---- helpers ----

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
