// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using PackageUploader.ClientApi.Client.Ingestion.Client;
using PackageUploader.ClientApi.Client.Ingestion.Mappers;
using PackageUploader.ClientApi.Client.Ingestion.Models;
using System.Text.Json;

namespace PackageUploader.ClientApi.Test;

[TestClass]
public class PackageMetadataTest
{
    [TestMethod]
    public void ProcessingRequest_DoesNotEchoResponseMetadata_AndPreservesUploadInfo()
    {
        var response = JsonSerializer.Deserialize("""
            {
              "id": "package-resource",
              "state": "PendingUpload",
              "packageType": "Msixvc",
              "xvcTargetPlatform": "PC",
              "buildId": "11111111-2222-4333-8444-555555555555",
              "contentId": "aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee",
              "version": "1.0.0.0",
              "uploadInfo": {
                "xfusId": "12345678-1234-4234-8234-123456789012",
                "token": "synthetic-upload-token",
                "uploadDomain": "https://upload.example.test",
                "xfusTenant": "synthetic-tenant"
              }
            }
            """, IngestionJsonSerializerContext.Default.IngestionGamePackage);
        var package = response.Map();
        package.State = GamePackageState.Uploaded;

        var request = package.Map();
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(request, IngestionJsonSerializerContext.Default.IngestionGamePackage));
        var root = document.RootElement;
        Assert.AreEqual("Uploaded", root.GetProperty("State").GetString());
        Assert.AreEqual("package-resource", root.GetProperty("Id").GetString());
        Assert.AreEqual("synthetic-upload-token", root.GetProperty("UploadInfo").GetProperty("Token").GetString());
        foreach (var field in new[] { "PackageType", "XvcTargetPlatform", "BuildId", "ContentId", "Version" })
        {
            Assert.IsFalse(root.TryGetProperty(field, out _), $"{field} must remain response-only.");
        }
    }
}
