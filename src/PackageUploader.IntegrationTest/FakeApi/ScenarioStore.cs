// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net.Http;
using System.Text.RegularExpressions;

namespace PackageUploader.IntegrationTest.FakeApi;

/// <summary>A scripted response: an HTTP status code and an optional JSON body object.</summary>
public sealed record FakeResponse(int StatusCode, object? Body = null);

/// <summary>A request observed by the fake API, for test assertions.</summary>
public sealed record RecordedRequest(
    HttpMethod Method,
    string Path,
    IReadOnlyDictionary<string, string> Headers,
    string? Body);

/// <summary>
/// Per-test configurable store of scripted responses for one fake service. Tests register stubs
/// (success / error / retry / polling) before exercising the client; the matching controller calls
/// <see cref="Resolve"/> for each incoming request and <see cref="Record"/> to log it. Rules match by
/// HTTP method and a path pattern where <c>*</c> matches any run of non-slash characters; rules are
/// evaluated in registration order, first match wins, and sequential rules advance per call and stay
/// on the final response.
/// </summary>
public abstract class ScenarioStore
{
    private readonly List<Rule> _rules = [];
    private readonly List<RecordedRequest> _received = [];
    private readonly Lock _lock = new();

    /// <summary>Every request observed by the fake service, in order, for assertions.</summary>
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

    protected void On(HttpMethod method, string pathPattern, Func<FakeResponse> respond)
    {
        var regex = ToRegex(pathPattern);
        lock (_lock)
        {
            _rules.Add(new Rule(method, regex, () => respond()));
        }
    }

    protected void OnSequence(HttpMethod method, string pathPattern, IReadOnlyList<Func<FakeResponse>> responders)
    {
        var regex = ToRegex(pathPattern);
        var index = 0;
        lock (_lock)
        {
            _rules.Add(new Rule(method, regex, () =>
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

    /// <summary>Returns the scripted response for a request, or 400 if no stub matches.</summary>
    public FakeResponse Resolve(string method, string absolutePath)
    {
        var parsed = HttpMethod.Parse(method);
        lock (_lock)
        {
            var rule = _rules.FirstOrDefault(r => r.Matches(parsed, absolutePath));
            return rule is null ? new FakeResponse(400) : rule.Respond();
        }
    }

    /// <summary>Records an observed request.</summary>
    public void Record(string method, string path, IReadOnlyDictionary<string, string> headers, string? body)
    {
        lock (_lock)
        {
            _received.Add(new RecordedRequest(HttpMethod.Parse(method), path, headers, body));
        }
    }

    private static Regex ToRegex(string pathPattern)
    {
        var escaped = Regex.Escape(pathPattern).Replace("\\*", "[^/]*");
        return new Regex("^" + escaped + "$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    }

    private sealed class Rule(HttpMethod method, Regex pathRegex, Func<FakeResponse> respond)
    {
        public bool Matches(HttpMethod method2, string absolutePath) =>
            method2 == method && pathRegex.IsMatch(absolutePath);

        public FakeResponse Respond() => respond();
    }
}
