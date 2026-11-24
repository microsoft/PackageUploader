// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;
using PackageUploader.ClientApi.Client.Ingestion;
using PackageUploader.ClientApi.Client.Ingestion.Builders;
using PackageUploader.ClientApi.Client.Ingestion.Client;
using PackageUploader.ClientApi.Client.Ingestion.Models;
using PackageUploader.ClientApi.Client.Ingestion.Models.Internal;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace PackageUploader.ClientApi.Test;

/// <summary>
/// Tests that UploadSource is correctly propagated through ClientExtractedMetaData
/// in the XVC1 package creation flow.
/// </summary>
[TestClass]
public class UploadSourceClientExtractedMetaDataTests
{
    #region UploadSourceConfig — makepkg2 allowlist

    [TestMethod]
    public void IsAllowedValue_MakePkg2_ReturnsTrue()
    {
        Assert.IsTrue(UploadSourceConfig.IsAllowedValue("makepkg2"));
    }

    [TestMethod]
    [DataRow("MAKEPKG2")]
    [DataRow("MakePkg2")]
    [DataRow("Makepkg2")]
    [DataRow("makePKG2")]
    public void IsAllowedValue_MakePkg2CaseInsensitive_ReturnsTrue(string variant)
    {
        Assert.IsTrue(UploadSourceConfig.IsAllowedValue(variant),
            $"Case variant '{variant}' should be accepted");
    }

    [TestMethod]
    public void MakePkg2Source_ConstantValue_IsMakepkg2()
    {
        Assert.AreEqual("makepkg2", UploadSourceConfig.MakePkg2Source);
    }

    [TestMethod]
    public void MakePkg2UploadSource_PublicConstant_MatchesInternal()
    {
        Assert.AreEqual(UploadSourceConfig.MakePkg2Source, IngestionExtensions.MakePkg2UploadSource);
    }

    [TestMethod]
    public void IsAllowedValue_AllThreeValues_Accepted()
    {
        Assert.IsTrue(UploadSourceConfig.IsAllowedValue("PackageUploader"));
        Assert.IsTrue(UploadSourceConfig.IsAllowedValue("XGPM"));
        Assert.IsTrue(UploadSourceConfig.IsAllowedValue("makepkg2"));
    }

    #endregion

    #region IngestionPackageCreationRequestBuilder — UploadSource in ClientExtractedMetaData

    [TestMethod]
    [DataRow("PackageUploader")]
    [DataRow("XGPM")]
    [DataRow("makepkg2")]
    public void Build_XvcPackage_IncludesUploadSource(string uploadSource)
    {
        var builder = new IngestionPackageCreationRequestBuilder(
            "draftId", "test.xvc", "marketGroupId",
            ixXvc: true, XvcTargetPlatform.PC, uploadSource: uploadSource);

        var request = builder.Build();

        Assert.IsNotNull(request.ClientExtractedMetaData,
            "ClientExtractedMetaData should be set for XVC packages");
        Assert.AreEqual(uploadSource, request.ClientExtractedMetaData.UploadSource,
            $"UploadSource should be '{uploadSource}'");
    }

    [TestMethod]
    public void Build_XvcPackage_NullUploadSource_FieldIsNull()
    {
        var builder = new IngestionPackageCreationRequestBuilder(
            "draftId", "test.xvc", "marketGroupId",
            ixXvc: true, XvcTargetPlatform.PC, uploadSource: null);

        var request = builder.Build();

        Assert.IsNotNull(request.ClientExtractedMetaData);
        Assert.IsNull(request.ClientExtractedMetaData.UploadSource,
            "UploadSource should be null when not provided");
    }

    [TestMethod]
    public void Build_XvcPackage_DefaultParameter_UploadSourceIsNull()
    {
        // When uploadSource parameter is omitted (default = null)
        var builder = new IngestionPackageCreationRequestBuilder(
            "draftId", "test.xvc", "marketGroupId",
            ixXvc: true, XvcTargetPlatform.PC);

        var request = builder.Build();

        Assert.IsNotNull(request.ClientExtractedMetaData);
        Assert.IsNull(request.ClientExtractedMetaData.UploadSource,
            "UploadSource should default to null when parameter is omitted");
    }

    [TestMethod]
    public void Build_NonXvcPackage_ClientExtractedMetaDataIsNull()
    {
        var builder = new IngestionPackageCreationRequestBuilder(
            "draftId", "test.appx", "marketGroupId",
            ixXvc: false, XvcTargetPlatform.NotSpecified, uploadSource: "XGPM");

        var request = builder.Build();

        Assert.IsNull(request.ClientExtractedMetaData,
            "Non-XVC packages should NOT have ClientExtractedMetaData, regardless of uploadSource");
    }

    [TestMethod]
    public void Build_XvcPackage_PreservesXvcReaderFields()
    {
        var builder = new IngestionPackageCreationRequestBuilder(
            "draftId", "test.xvc", "marketGroupId",
            ixXvc: true, XvcTargetPlatform.ConsoleGen9, uploadSource: "XGPM");

        var request = builder.Build();

        Assert.IsNotNull(request.ClientExtractedMetaData?.XvcReader);
        Assert.AreEqual(XvcTargetPlatform.ConsoleGen9, request.ClientExtractedMetaData.XvcReader.XvcTargetPlatform,
            "XvcTargetPlatform should be preserved");
        Assert.AreEqual(string.Empty, request.ClientExtractedMetaData.XvcReader.GameConfig,
            "GameConfig should remain empty string");
        Assert.AreEqual("XGPM", request.ClientExtractedMetaData.UploadSource);
    }

    [TestMethod]
    public void Build_XvcPackage_EmptyStringUploadSource_SetsEmptyString()
    {
        var builder = new IngestionPackageCreationRequestBuilder(
            "draftId", "test.xvc", "marketGroupId",
            ixXvc: true, XvcTargetPlatform.PC, uploadSource: "");

        var request = builder.Build();

        Assert.IsNotNull(request.ClientExtractedMetaData);
        Assert.AreEqual("", request.ClientExtractedMetaData.UploadSource,
            "Builder should pass empty string through without modification");
    }

    [TestMethod]
    public void Build_PreservesOtherRequestFields()
    {
        var builder = new IngestionPackageCreationRequestBuilder(
            "myDraftId", "game.xvc", "myMarketGroup",
            ixXvc: true, XvcTargetPlatform.PC, uploadSource: "makepkg2");

        var request = builder.Build();

        Assert.AreEqual("myDraftId", request.PackageConfigurationId);
        Assert.AreEqual("game.xvc", request.FileName);
        Assert.AreEqual("myMarketGroup", request.MarketGroupId);
        Assert.AreEqual("PackageCreationRequest", request.ResourceType);
        Assert.AreEqual("makepkg2", request.ClientExtractedMetaData.UploadSource);
    }

    #endregion

    #region ClientExtractedMetaData model

    [TestMethod]
    public void ClientExtractedMetaData_DefaultUploadSource_IsNull()
    {
        var metadata = new ClientExtractedMetaData();
        Assert.IsNull(metadata.UploadSource);
    }

    [TestMethod]
    public void ClientExtractedMetaData_SetUploadSource_RoundTrips()
    {
        var metadata = new ClientExtractedMetaData { UploadSource = "XGPM" };
        Assert.AreEqual("XGPM", metadata.UploadSource);
    }

    #endregion

    #region JSON serialization — UploadSource in ClientExtractedMetaData

    [TestMethod]
    public void Serialization_NullUploadSource_OmittedFromJson()
    {
        var metadata = new ClientExtractedMetaData
        {
            XvcReader = new XvcReader
            {
                XvcTargetPlatform = XvcTargetPlatform.PC,
                GameConfig = string.Empty,
            },
            UploadSource = null,
        };

        var request = new IngestionPackageCreationRequest
        {
            ClientExtractedMetaData = metadata,
        };

        string json = JsonSerializer.Serialize(request,
            IngestionJsonSerializerContext.Default.IngestionPackageCreationRequest);

        Assert.IsFalse(json.Contains("UploadSource", StringComparison.OrdinalIgnoreCase),
            $"Null UploadSource should be omitted from JSON. Got: {json}");
    }

    [TestMethod]
    [DataRow("PackageUploader")]
    [DataRow("XGPM")]
    [DataRow("makepkg2")]
    public void Serialization_ValidUploadSource_IncludedInJson(string uploadSource)
    {
        var metadata = new ClientExtractedMetaData
        {
            XvcReader = new XvcReader
            {
                XvcTargetPlatform = XvcTargetPlatform.PC,
                GameConfig = string.Empty,
            },
            UploadSource = uploadSource,
        };

        var request = new IngestionPackageCreationRequest
        {
            ClientExtractedMetaData = metadata,
        };

        string json = JsonSerializer.Serialize(request,
            IngestionJsonSerializerContext.Default.IngestionPackageCreationRequest);

        Assert.IsTrue(json.Contains($"\"UploadSource\":\"{uploadSource}\"", StringComparison.Ordinal),
            $"UploadSource '{uploadSource}' should appear in JSON. Got: {json}");
    }

    [TestMethod]
    public void Serialization_NoClientExtractedMetaData_OmittedFromJson()
    {
        // Non-XVC scenario: no ClientExtractedMetaData at all
        var request = new IngestionPackageCreationRequest
        {
            PackageConfigurationId = "draftId",
            FileName = "test.appx",
            MarketGroupId = "marketGroup",
            ClientExtractedMetaData = null,
        };

        string json = JsonSerializer.Serialize(request,
            IngestionJsonSerializerContext.Default.IngestionPackageCreationRequest);

        Assert.IsFalse(json.Contains("ClientExtractedMetaData", StringComparison.OrdinalIgnoreCase),
            $"Null ClientExtractedMetaData should be omitted. Got: {json}");
        Assert.IsFalse(json.Contains("UploadSource", StringComparison.OrdinalIgnoreCase),
            $"UploadSource should not appear when metadata is null. Got: {json}");
    }

    [TestMethod]
    public void Serialization_RoundTrip_PreservesUploadSource()
    {
        var original = new IngestionGamePackage
        {
            ClientExtractedMetaData = new ClientExtractedMetaData
            {
                XvcReader = new XvcReader
                {
                    XvcTargetPlatform = XvcTargetPlatform.ConsoleGen9,
                    GameConfig = "",
                },
                UploadSource = "makepkg2",
            },
            State = "PendingUpload",
        };

        string json = JsonSerializer.Serialize(original,
            IngestionJsonSerializerContext.Default.IngestionGamePackage);

        var deserialized = JsonSerializer.Deserialize(json,
            IngestionJsonSerializerContext.Default.IngestionGamePackage);

        Assert.IsNotNull(deserialized?.ClientExtractedMetaData);
        Assert.AreEqual("makepkg2", deserialized.ClientExtractedMetaData.UploadSource);
        Assert.AreEqual(XvcTargetPlatform.ConsoleGen9,
            deserialized.ClientExtractedMetaData.XvcReader.XvcTargetPlatform);
    }

    #endregion

    #region IngestionHttpClient — UploadSource flows into request body

    /// <summary>
    /// Creates an IngestionHttpClient with a mock handler that captures the request body,
    /// then calls CreatePackageRequestAsync with isXvc=true.
    /// </summary>
    private static async Task<(string RequestBody, IngestionGamePackage Response)>
        CaptureCreatePackageRequestBodyAsync(string uploadSourceConfigValue)
    {
        string capturedBody = null;

        var responsePackage = new IngestionGamePackage
        {
            Id = "pkg-123",
            State = "PendingUpload",
            UploadInfo = new IngestionXfusUploadInfo { XfusId = Guid.NewGuid().ToString() },
        };

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>(async (req, _) =>
            {
                if (req.Content != null)
                {
                    capturedBody = await req.Content.ReadAsStringAsync();
                }
            })
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(responsePackage),
            });

        var httpClient = new HttpClient(handler.Object)
        {
            BaseAddress = new Uri("https://test.example.com/"),
        };

        var config = uploadSourceConfigValue != null
            ? new UploadSourceConfig { UploadSource = uploadSourceConfigValue }
            : null;

        var client = new IngestionHttpClient(
            new NullLogger<IngestionHttpClient>(), httpClient, null, config);

        await client.CreatePackageRequestAsync(
            "productId", "draftId", "game.xvc", "marketGroupId",
            isXvc: true, XvcTargetPlatform.PC, CancellationToken.None);

        return (capturedBody, responsePackage);
    }

    [TestMethod]
    public async Task CreatePackageRequest_XvcWithXgpmConfig_BodyContainsUploadSource()
    {
        var (body, _) = await CaptureCreatePackageRequestBodyAsync("XGPM");

        Assert.IsNotNull(body, "Request body should not be null");
        Assert.IsTrue(body.Contains("\"UploadSource\":\"XGPM\"", StringComparison.Ordinal),
            $"Body should contain UploadSource=XGPM. Got: {body}");
    }

    [TestMethod]
    public async Task CreatePackageRequest_XvcWithPackageUploaderConfig_BodyContainsUploadSource()
    {
        var (body, _) = await CaptureCreatePackageRequestBodyAsync("PackageUploader");

        Assert.IsNotNull(body);
        Assert.IsTrue(body.Contains("\"UploadSource\":\"PackageUploader\"", StringComparison.Ordinal),
            $"Body should contain UploadSource=PackageUploader. Got: {body}");
    }

    [TestMethod]
    public async Task CreatePackageRequest_XvcWithMakePkg2Config_BodyContainsUploadSource()
    {
        var (body, _) = await CaptureCreatePackageRequestBodyAsync("makepkg2");

        Assert.IsNotNull(body);
        Assert.IsTrue(body.Contains("\"UploadSource\":\"makepkg2\"", StringComparison.Ordinal),
            $"Body should contain UploadSource=makepkg2. Got: {body}");
    }

    [TestMethod]
    public async Task CreatePackageRequest_XvcWithNullConfig_DefaultsToPackageUploader()
    {
        // When config is null, HttpRestClient defaults to "PackageUploader"
        var (body, _) = await CaptureCreatePackageRequestBodyAsync(null);

        Assert.IsNotNull(body);
        Assert.IsTrue(body.Contains("\"UploadSource\":\"PackageUploader\"", StringComparison.Ordinal),
            $"Null config should default to PackageUploader in body. Got: {body}");
    }

    [TestMethod]
    public async Task CreatePackageRequest_XvcWithInvalidConfig_DefaultsToPackageUploader()
    {
        // Invalid source falls back to "PackageUploader" in HttpRestClient
        var (body, _) = await CaptureCreatePackageRequestBodyAsync("EvilSource");

        Assert.IsNotNull(body);
        Assert.IsTrue(body.Contains("\"UploadSource\":\"PackageUploader\"", StringComparison.Ordinal),
            $"Invalid config should fall back to PackageUploader in body. Got: {body}");
    }

    [TestMethod]
    public async Task CreatePackageRequest_XvcBody_ContainsBothXvcReaderAndUploadSource()
    {
        var (body, _) = await CaptureCreatePackageRequestBodyAsync("XGPM");

        Assert.IsNotNull(body);
        Assert.IsTrue(body.Contains("XvcReader", StringComparison.OrdinalIgnoreCase),
            "Body should contain XvcReader");
        Assert.IsTrue(body.Contains("XvcTargetPlatform", StringComparison.OrdinalIgnoreCase),
            "Body should contain XvcTargetPlatform");
        Assert.IsTrue(body.Contains("UploadSource", StringComparison.OrdinalIgnoreCase),
            "Body should contain UploadSource");
    }

    [TestMethod]
    public async Task CreatePackageRequest_UploadSource_InBodyAndHeader()
    {
        // Verify that UploadSource appears in BOTH the header AND the body
        string capturedBody = null;
        HttpRequestMessage capturedRequest = null;

        var responsePackage = new IngestionGamePackage
        {
            Id = "pkg-123",
            State = "PendingUpload",
            UploadInfo = new IngestionXfusUploadInfo { XfusId = Guid.NewGuid().ToString() },
        };

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>(async (req, _) =>
            {
                capturedRequest = req;
                if (req.Content != null)
                {
                    capturedBody = await req.Content.ReadAsStringAsync();
                }
            })
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(responsePackage),
            });

        var httpClient = new HttpClient(handler.Object)
        {
            BaseAddress = new Uri("https://test.example.com/"),
        };

        var config = new UploadSourceConfig { UploadSource = "XGPM" };
        var client = new IngestionHttpClient(
            new NullLogger<IngestionHttpClient>(), httpClient, null, config);

        await client.CreatePackageRequestAsync(
            "productId", "draftId", "game.xvc", "marketGroupId",
            isXvc: true, XvcTargetPlatform.PC, CancellationToken.None);

        // Verify header
        Assert.IsNotNull(capturedRequest);
        var headerValues = capturedRequest.Headers.GetValues("UploadSource").ToArray();
        Assert.AreEqual("XGPM", headerValues[0], "Header should contain XGPM");

        // Verify body
        Assert.IsNotNull(capturedBody);
        Assert.IsTrue(capturedBody.Contains("\"UploadSource\":\"XGPM\""),
            "Body should also contain UploadSource=XGPM");
    }

    #endregion

    #region UploadSourceConfig — HttpRestClient fallback for makepkg2

    [TestMethod]
    public void UploadSourceHeader_MakePkg2Value_IsAccepted()
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
                Content = JsonContent.Create(new IngestionGameProduct { Id = "test" })
            });

        var httpClient = new HttpClient(handler.Object)
        {
            BaseAddress = new Uri("https://test.example.com/"),
        };

        var config = new UploadSourceConfig { UploadSource = "makepkg2" };
        var client = new IngestionHttpClient(
            new NullLogger<IngestionHttpClient>(), httpClient, null, config);

        try
        {
            client.GetGameProductByLongIdAsync("test", CancellationToken.None)
                .GetAwaiter().GetResult();
        }
        catch { }

        Assert.IsNotNull(capturedRequest);
        var values = capturedRequest.Headers.GetValues("UploadSource").ToArray();
        Assert.AreEqual("makepkg2", values[0],
            "makepkg2 should be accepted by the allowlist for header");
    }

    [TestMethod]
    [DataRow("makepkg2", "makepkg2")]
    [DataRow("MAKEPKG2", "MAKEPKG2")]
    [DataRow("  makepkg2  ", "makepkg2")]
    public void UploadSourceHeader_MakePkg2Variants_Accepted(string input, string expected)
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
                Content = JsonContent.Create(new IngestionGameProduct { Id = "test" })
            });

        var httpClient = new HttpClient(handler.Object)
        {
            BaseAddress = new Uri("https://test.example.com/"),
        };

        var config = new UploadSourceConfig { UploadSource = input };
        var client = new IngestionHttpClient(
            new NullLogger<IngestionHttpClient>(), httpClient, null, config);

        try
        {
            client.GetGameProductByLongIdAsync("test", CancellationToken.None)
                .GetAwaiter().GetResult();
        }
        catch { }

        Assert.IsNotNull(capturedRequest);
        var value = capturedRequest.Headers.GetValues("UploadSource").First();
        Assert.AreEqual(expected, value);
    }

    #endregion

    #region DI integration — AddPackageUploaderService with makepkg2

    [TestMethod]
    public void AddPackageUploaderService_WithMakePkg2Source_RegistersConfig()
    {
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        services.AddPackageUploaderService(uploadSource: IngestionExtensions.MakePkg2UploadSource);

        // Verify the UploadSourceConfig singleton was registered with "makepkg2"
        var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(UploadSourceConfig));
        Assert.IsNotNull(descriptor, "UploadSourceConfig should be registered");
        Assert.AreEqual(Microsoft.Extensions.DependencyInjection.ServiceLifetime.Singleton, descriptor.Lifetime);

        var config = descriptor.ImplementationInstance as UploadSourceConfig;
        Assert.IsNotNull(config, "UploadSourceConfig should be registered as an instance");
        Assert.AreEqual("makepkg2", config.UploadSource);
    }

    #endregion
}
