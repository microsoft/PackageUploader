// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PackageUploader.ClientApi.Client.Ingestion;
using PackageUploader.ClientApi.Client.Ingestion.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PackageUploader.ClientApi.Test;

/// <summary>
/// Wire-level integration tests that verify the UploadSource header actually
/// travels over a real TCP connection.
/// </summary>
[TestClass]
[TestCategory("Integration")]
public class IngestionHttpClientWireTests
{
    private static async Task<(Dictionary<string, string> Headers, List<string> RawHeaderLines)>
        CaptureWireRequestAsync(TcpListener listener, TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        using var tcpClient = await listener.AcceptTcpClientAsync(cts.Token);
        using var stream = tcpClient.GetStream();
        using var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true);

        var rawHeaderLines = new List<string>();
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Read request line
        string requestLine = await reader.ReadLineAsync(cts.Token);

        // Read headers until blank line
        while (true)
        {
            string line = await reader.ReadLineAsync(cts.Token);
            if (string.IsNullOrEmpty(line)) break;
            rawHeaderLines.Add(line);
            int ci = line.IndexOf(":");
            if (ci > 0)
            {
                headers[line[..ci].Trim()] = line[(ci + 1)..].Trim();
            }
        }

        // Send minimal 200 OK with JSON body
        const string body = "{\"id\":\"wire-test\"}";
        string resp = "HTTP/1.1 200 OK\r\n" +
            "Content-Type: application/json\r\n" +
            $"Content-Length: {Encoding.UTF8.GetByteCount(body)}\r\n" +
            "Connection: close\r\n" +
            "\r\n" + body;
        await stream.WriteAsync(Encoding.UTF8.GetBytes(resp), cts.Token);
        await stream.FlushAsync(cts.Token);

        return (headers, rawHeaderLines);
    }

    private static async Task<(Dictionary<string, string> Headers, List<string> RawHeaderLines)>
        SendRequestAndCaptureHeaders(UploadSourceConfig config)
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        try
        {
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            var serverTask = CaptureWireRequestAsync(listener, TimeSpan.FromSeconds(10));

            using var httpClient = new HttpClient
            {
                BaseAddress = new Uri($"http://127.0.0.1:{port}/"),
                Timeout = TimeSpan.FromSeconds(10),
            };

            var client = new IngestionHttpClient(
                new NullLogger<IngestionHttpClient>(), httpClient, null, config);

            try
            {
                await client.GetGameProductByLongIdAsync("wire-test", CancellationToken.None);
            }
            catch { }

            return await serverTask.WaitAsync(TimeSpan.FromSeconds(10));
        }
        finally
        {
            listener.Stop();
        }
    }

    #region Wire-Level UploadSource Tests

    [TestMethod]
    public async Task WireLevel_DefaultConfig_SendsPackageUploaderHeader()
    {
        var (headers, _) = await SendRequestAndCaptureHeaders(null);

        Assert.IsTrue(headers.ContainsKey("UploadSource"),
            "UploadSource header must be present on the wire. " +
            "Headers: " + string.Join(", ", headers.Keys));

        Assert.AreEqual("PackageUploader", headers["UploadSource"],
            "Default UploadSource must be PackageUploader on the wire");
    }

    [TestMethod]
    public async Task WireLevel_XgpmConfig_SendsXgpmHeader()
    {
        var config = new UploadSourceConfig { UploadSource = "XGPM" };
        var (headers, _) = await SendRequestAndCaptureHeaders(config);

        Assert.IsTrue(headers.ContainsKey("UploadSource"),
            "UploadSource header must be present on the wire");

        Assert.AreEqual("XGPM", headers["UploadSource"],
            "UploadSource must be XGPM on the wire when configured");
    }

    [TestMethod]
    public async Task WireLevel_AllStandardHeaders_ArriveOnServer()
    {
        var config = new UploadSourceConfig { UploadSource = "XGPM" };
        var (headers, _) = await SendRequestAndCaptureHeaders(config);

        Assert.IsTrue(headers.ContainsKey("Request-ID"),
            "Request-ID must be present on the wire");
        Assert.IsFalse(string.IsNullOrWhiteSpace(headers["Request-ID"]),
            "Request-ID must have a non-empty value");

        Assert.IsTrue(headers.ContainsKey("MethodOfAccess"),
            "MethodOfAccess must be present on the wire");

        Assert.IsTrue(headers.ContainsKey("UploadSource"));
        Assert.AreEqual("XGPM", headers["UploadSource"]);
    }

    [TestMethod]
    public async Task WireLevel_UploadSourceHeader_AppearsExactlyOnce()
    {
        var config = new UploadSourceConfig { UploadSource = "XGPM" };
        var (_, rawHeaderLines) = await SendRequestAndCaptureHeaders(config);

        int count = rawHeaderLines
            .Count(l => l.StartsWith("UploadSource:", StringComparison.OrdinalIgnoreCase));

        Assert.AreEqual(1, count,
            $"UploadSource header must appear exactly once. Found {count}.");
    }

    #endregion
}