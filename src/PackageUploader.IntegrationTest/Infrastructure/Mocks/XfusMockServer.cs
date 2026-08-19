// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace PackageUploader.IntegrationTest.Infrastructure.Mocks;

/// <summary>
/// A reusable WireMock.Net-backed fake of the XFUS upload service. Serves the three-step chunked
/// upload conversation PackageUploader performs — initialize, per-block payload PUT, and continue —
/// for both the no-delta and delta upload paths, with configurable success / error / retry
/// scenarios. Dispose to stop the server.
/// </summary>
/// <remarks>
/// The real client's base address is <c>{UploadDomain}/api/v2/assets/</c>, so all stubs are rooted
/// at <c>/api/v2/assets/</c>. Responses omit <c>directUploadParameters.sasUri</c> so the client
/// uploads blocks via the proxy PUT path (which this server serves) rather than to Azure blob
/// storage. The <c>status</c> field is emitted as a number because the client's serializer has no
/// string-enum converter (ReceivingBlocks=0, Busy=1, Completed=2).
/// </remarks>
internal sealed class XfusMockServer : IDisposable
{
    private const string AssetsRoot = "/api/v2/assets";

    private readonly WireMockServer _server;
    private readonly string _id = Guid.NewGuid().ToString("N");

    public XfusMockServer() => _server = WireMockServer.Start();

    /// <summary>Base URL of the running mock server (e.g. http://localhost:12345).</summary>
    public string Url => _server.Url!;

    /// <summary>The underlying server, for advanced stubbing or request-log assertions.</summary>
    public WireMockServer Server => _server;

    /// <summary>
    /// Configures a complete, successful no-delta upload: initialize returns the given blocks in the
    /// ReceivingBlocks state, every block payload PUT succeeds, and continue reports Completed.
    /// </summary>
    public XfusMockServer StubNoDeltaUploadSuccess(params long[] blockSizes)
    {
        var sizes = blockSizes.Length > 0 ? blockSizes : [64L * 1024];
        StubInitialize(UploadProgress(sizes, XfusStatus.ReceivingBlocks));
        StubBlockUpload();
        StubContinue(UploadProgress([], XfusStatus.Completed));
        return this;
    }

    /// <summary>Stubs POST .../{assetId}/initialize with the given JSON upload-progress body.</summary>
    public XfusMockServer StubInitialize(object uploadProgressBody)
    {
        _server
            .Given(Request.Create().WithPath($"{AssetsRoot}/*/initialize").UsingPost())
            .RespondWith(JsonResponse(uploadProgressBody));
        return this;
    }

    /// <summary>Stubs PUT .../{assetId}/blocks/{blockId}/source/payload to accept block bytes.</summary>
    public XfusMockServer StubBlockUpload(HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        _server
            .Given(Request.Create().WithPath($"{AssetsRoot}/*/blocks/*/source/payload").UsingPut())
            .RespondWith(Response.Create().WithStatusCode((int)statusCode));
        return this;
    }

    /// <summary>Stubs POST .../{assetId}/continue with the given JSON upload-progress body.</summary>
    public XfusMockServer StubContinue(object uploadProgressBody)
    {
        _server
            .Given(Request.Create().WithPath($"{AssetsRoot}/*/continue").UsingPost())
            .RespondWith(JsonResponse(uploadProgressBody));
        return this;
    }

    /// <summary>
    /// Returns the given continue-progress bodies in order across successive calls, then stays on the
    /// final one — used to simulate the server working through the upload (e.g. Busy then Completed)
    /// or the multi-step delta plan. The progression is keyed per server instance, so it assumes a
    /// single asset upload per <see cref="XfusMockServer"/> (the usual one-upload-per-test pattern).
    /// </summary>
    public XfusMockServer StubContinueProgression(params object[] uploadProgressBodies)
    {
        var bodies = uploadProgressBodies.Length > 0 ? uploadProgressBodies : [UploadProgress([], XfusStatus.Completed)];
        var request = Request.Create().WithPath($"{AssetsRoot}/*/continue").UsingPost();
        var scenario = $"xfus-continue-{_id}";

        for (int i = 0; i < bodies.Length; i++)
        {
            bool isLast = i == bodies.Length - 1;
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
            builder.RespondWith(JsonResponse(bodies[i]));
        }

        return this;
    }

    /// <summary>Always responds to the given method/path-wildcard with the supplied status code.</summary>
    public XfusMockServer StubError(string method, string pathWildcard, HttpStatusCode statusCode)
    {
        _server
            .Given(Request.Create().WithPath(pathWildcard).UsingMethod(method))
            .RespondWith(Response.Create().WithStatusCode((int)statusCode));
        return this;
    }

    /// <summary>
    /// Fails the first <paramref name="failures"/> block payload PUTs with <paramref name="failureStatus"/>,
    /// then accepts subsequent ones — used to exercise the uploader's block re-upload behaviour.
    /// </summary>
    public XfusMockServer StubBlockUploadRetryThenSuccess(int failures = 1, HttpStatusCode failureStatus = HttpStatusCode.ServiceUnavailable)
    {
        var request = Request.Create().WithPath($"{AssetsRoot}/*/blocks/*/source/payload").UsingPut();
        var scenario = $"xfus-block-{_id}";

        if (failures <= 0)
        {
            _server.Given(request).RespondWith(Response.Create().WithStatusCode((int)HttpStatusCode.OK));
            return this;
        }

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
            .WillSetStateTo(StateName(failures))
            .RespondWith(Response.Create().WithStatusCode((int)HttpStatusCode.OK));
        return this;
    }

    public void Dispose() => _server.Stop();

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

    private static string StateName(int index) => $"step-{index}";

    private static IResponseBuilder JsonResponse(object body) =>
        Response.Create()
            .WithStatusCode(200)
            .WithHeader("Content-Type", "application/json")
            .WithBodyAsJson(body);
}
