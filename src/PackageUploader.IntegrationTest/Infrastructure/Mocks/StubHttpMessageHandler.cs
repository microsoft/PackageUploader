// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace PackageUploader.IntegrationTest.Infrastructure.Mocks;

/// <summary>
/// In-memory <see cref="HttpMessageHandler"/> that returns scripted responses and records requests.
/// This is the first-party (BCL-only) equivalent of a mock HTTP server: it is plugged in as the
/// primary handler of the client's named <see cref="HttpClient"/>, so the real pipeline (auth
/// handler, Polly policies, serialization) runs against deterministic in-memory responses — no
/// network, no external dependency.
/// <para>
/// Rules are matched by HTTP method and a path pattern where <c>*</c> matches any run of non-slash
/// characters. Rules are evaluated in registration order; the first match wins. Sequential rules
/// return their responses in order across successive calls and then stay on the final one.
/// Unmatched requests return a non-transient 400 so a missing stub fails fast rather than being
/// retried by the client's Polly policy.
/// </para>
/// </summary>
internal abstract class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly List<Rule> _rules = [];
    private readonly List<RecordedRequest> _received = [];
    private readonly Lock _lock = new();

    /// <summary>Every request observed by the handler, in order, for assertions.</summary>
    public IReadOnlyList<RecordedRequest> ReceivedRequests
    {
        get
        {
            lock (_lock)
            {
                return _received.ToArray();
            }
        }
    }

    /// <summary>Registers a single response for requests matching the method and path pattern.</summary>
    protected void On(HttpMethod method, string pathPattern, Func<HttpResponseMessage> respond)
    {
        var regex = ToRegex(pathPattern);
        lock (_lock)
        {
            _rules.Add(new Rule(method, regex, _ => respond()));
        }
    }

    /// <summary>
    /// Registers a sequence of responses for matching requests: each call returns the next response,
    /// and the final one is repeated for all subsequent calls (used for polling and retry).
    /// </summary>
    protected void OnSequence(HttpMethod method, string pathPattern, IReadOnlyList<Func<HttpResponseMessage>> responders)
    {
        var regex = ToRegex(pathPattern);
        var index = 0;
        lock (_lock)
        {
            _rules.Add(new Rule(method, regex, _ =>
            {
                var responder = responders[Math.Min(index, responders.Count - 1)];
                if (index < responders.Count - 1)
                {
                    index++;
                }
                return responder();
            }));
        }
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string? body = request.Content is null
            ? null
            : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        Rule? rule;
        lock (_lock)
        {
            _received.Add(new RecordedRequest(request.Method, request.RequestUri!, CloneHeaders(request.Headers), body));
            rule = _rules.FirstOrDefault(r => r.Matches(request));
        }

        if (rule is null)
        {
            // Non-transient 400 so a missing stub fails fast instead of being retried by Polly.
            return new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                RequestMessage = request,
                Content = new StringContent(
                    $"No stub registered for {request.Method} {request.RequestUri?.AbsolutePath}",
                    Encoding.UTF8, "text/plain"),
            };
        }

        HttpResponseMessage response;
        lock (_lock)
        {
            response = rule.Respond(request);
        }
        response.RequestMessage ??= request;
        return response;
    }

    /// <summary>Builds a JSON response with the given status code.</summary>
    protected static HttpResponseMessage Json(object body, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status)
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"),
        };

    /// <summary>Builds an empty response with the given status code.</summary>
    protected static HttpResponseMessage Status(HttpStatusCode status) => new(status);

    private static Regex ToRegex(string pathPattern)
    {
        var escaped = Regex.Escape(pathPattern).Replace("\\*", "[^/]*");
        return new Regex("^" + escaped + "$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
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

    private sealed class Rule(HttpMethod method, Regex pathRegex, Func<HttpRequestMessage, HttpResponseMessage> respond)
    {
        public bool Matches(HttpRequestMessage request) =>
            request.Method == method &&
            request.RequestUri is not null &&
            pathRegex.IsMatch(request.RequestUri.AbsolutePath);

        public HttpResponseMessage Respond(HttpRequestMessage request) => respond(request);
    }
}

/// <summary>Snapshot of a request observed by a <see cref="StubHttpMessageHandler"/>.</summary>
internal sealed record RecordedRequest(
    HttpMethod Method,
    Uri Uri,
    IReadOnlyDictionary<string, string> Headers,
    string? Body);
