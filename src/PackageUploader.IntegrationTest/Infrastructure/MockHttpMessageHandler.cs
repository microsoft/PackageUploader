// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace PackageUploader.IntegrationTest.Infrastructure;

/// <summary>In-process HTTP handler that returns scripted responses and records requests, backing the mock integration suite.</summary>
internal sealed class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly List<Responder> _responders = [];
    private readonly List<RecordedRequest> _received = [];
    private readonly Lock _receivedLock = new();

    public IReadOnlyList<RecordedRequest> ReceivedRequests
    {
        get
        {
            lock (_receivedLock)
            {
                return _received.ToArray();
            }
        }
    }

    public MockHttpMessageHandler When(HttpMethod method, string pathContains,
        Func<HttpRequestMessage, HttpResponseMessage> respond)
    {
        ArgumentNullException.ThrowIfNull(method);
        ArgumentNullException.ThrowIfNull(pathContains);
        ArgumentNullException.ThrowIfNull(respond);

        _responders.Add(new Responder(method, pathContains, respond));
        return this;
    }

    public MockHttpMessageHandler WhenJson(HttpMethod method, string pathContains, string json,
        HttpStatusCode status = HttpStatusCode.OK) =>
        When(method, pathContains, _ => new HttpResponseMessage(status)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        });

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        string? body = request.Content is null
            ? null
            : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        lock (_receivedLock)
        {
            _received.Add(new RecordedRequest(
                request.Method,
                request.RequestUri!,
                CloneHeaders(request.Headers),
                body));
        }

        var responder = _responders.FirstOrDefault(r => r.Matches(request));
        if (responder is null)
        {
            // Return a non-transient 4xx so a missing stub fails fast: the Ingestion pipeline's Polly
            // policy retries on >=500, which would otherwise turn a missing stub into slow retries.
            return new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                RequestMessage = request,
                Content = new StringContent(
                    $"No mock responder registered for {request.Method} {request.RequestUri}",
                    Encoding.UTF8, "text/plain"),
            };
        }

        var response = responder.Respond(request);
        response.RequestMessage ??= request;
        return response;
    }

    private static IReadOnlyDictionary<string, string> CloneHeaders(HttpRequestHeaders headers)
    {
        var clone = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var header in headers)
        {
            clone[header.Key] = string.Join(", ", header.Value);
        }
        return clone;
    }

    private sealed class Responder(HttpMethod method, string pathContains,
        Func<HttpRequestMessage, HttpResponseMessage> respond)
    {
        public bool Matches(HttpRequestMessage request) =>
            request.Method == method &&
            request.RequestUri is not null &&
            request.RequestUri.PathAndQuery.Contains(pathContains, StringComparison.OrdinalIgnoreCase);

        public HttpResponseMessage Respond(HttpRequestMessage request) => respond(request);
    }
}

/// <summary>Snapshot of a request observed by <see cref="MockHttpMessageHandler"/>.</summary>
internal sealed record RecordedRequest(
    HttpMethod Method,
    Uri Uri,
    IReadOnlyDictionary<string, string> Headers,
    string? Body);
