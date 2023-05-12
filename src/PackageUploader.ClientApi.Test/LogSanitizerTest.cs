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
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using PackageUploader.ClientApi.Client.Ingestion.Sanitizers;

namespace PackageUploader.ClientApi.Test;

[TestClass]
public class LogSanitizerTest
{

    [TestMethod]
    public void SanitizeGamePackageResponse()
    {
        const string response = 
            "{\"resourceType\":\"GamePackage\",\"packageType\":\"\",\"fileName\":\"Microsoft.GamePackage_1.1.1.0_x64_abcdefghij\",\"uploadInfo\":{\"fileName\":\"Microsoft.GamePackage_1.1.1.0_x64_abcdefghij\",\"xfusId\":\"12345678-1234-1234-1234-1234567890ab\",\"fileSasUri\":\"https://upload.xboxlive.com/upload/Asset/0123456789012345678?token=%3fsv%3d2021-01-01%26sr%3dc%26si%3d12345678-1234-1234-1234-1234567890ab2022-4-5-00-04-32%26sig%3d000000000000000000000000000000000000000000000000%26se%3d2022-04-10T00%253A37%253A32Z%26t%3dZZZ\",\"token\":\"?sv=2021-01-01%26sr%3dc%26si%3d12345678-1234-1234-1234-1234567890ab2022-4-5-00-04-32%26sig%3d000000000000000000000000000000000000000000000000%26se%3d2022-04-10T00%253A37%253A32Z%26t%3dZZZ\",\"uploadDomain\":\"https://upload.xboxlive.com\",\"xfusTenant\":\"ZZZ\"},\"state\":\"PendingUpload\",\"@odata.etag\":\"\"12345678-1234-1234-1234-1234567890ab\"\",\"id\":\"12345678-1234-1234-1234-1234567890ab\"}";
        const string expectedSanitizedResponse = 
            "{\"resourceType\":\"GamePackage\",\"packageType\":\"\",\"fileName\":\"Microsoft.GamePackage_1.1.1.0_x64_abcdefghij\",\"uploadInfo\":{\"fileName\":\"Microsoft.GamePackage_1.1.1.0_x64_abcdefghij\",\"xfusId\":\"12345678-1234-1234-1234-1234567890ab\",\"fileSasUri\":\"https://upload.xboxlive.com/upload/Asset/0123456789012345678?REDACTED\",\"token\":\"REDACTED\",\"uploadDomain\":\"https://upload.xboxlive.com\",\"xfusTenant\":\"ZZZ\"},\"state\":\"PendingUpload\",\"@odata.etag\":\"\"12345678-1234-1234-1234-1234567890ab\"\",\"id\":\"12345678-1234-1234-1234-1234567890ab\"}";

        var sanitizedResponse = LogSanitizer.SanitizeJsonResponse(response);

        Assert.AreEqual(expectedSanitizedResponse, sanitizedResponse);
    }

    [TestMethod]
    public void SanitizePackageAssetResponse()
    {
        const string response =
            "{\"resourceType\":\"PackageAsset\",\"type\":\"EraSymbolFile\",\"name\":\"Microsoft.GamePackage_1.1.1.0_x64_abcdefghij_erasymbol.zip\",\"isCommitted\":true,\"packageId\":\"12345678-1234-1234-1234-1234567890ab\",\"packageType\":\"\",\"createdDate\":\"0001-01-01T00:00:00Z\",\"binarySizeInBytes\":229689131,\"uploadInfo\":{\"fileName\":\"Microsoft.GamePackage_1.1.1.0_x64_abcdefghij_erasymbol.zip\",\"xfusId\":\"12345678-1234-1234-1234-1234567890ab\",\"fileSasUri\":\"https://bloburl.blob.core.windows.net/12345678-1234-1234-1234-1234567890ab/Microsoft.GamePackage_1.1.1.0_x64_abcdefghij_erasymbol.zip?sv=2021-01-01%26sr%3dc%26si%3d12345678-1234-1234-1234-1234567890ab2022-4-5-00-04-32%26sig%3d000000000000000000000000000000000000000000000000%26se%3d2022-04-10T00%253A37%253A32Z%26t%3dZZZ\",\"xfusTenant\":\"ZZZ\"},\"id\":\"EraSymbolFile\"}";
        const string expectedSanitizedResponse =
            "{\"resourceType\":\"PackageAsset\",\"type\":\"EraSymbolFile\",\"name\":\"Microsoft.GamePackage_1.1.1.0_x64_abcdefghij_erasymbol.zip\",\"isCommitted\":true,\"packageId\":\"12345678-1234-1234-1234-1234567890ab\",\"packageType\":\"\",\"createdDate\":\"0001-01-01T00:00:00Z\",\"binarySizeInBytes\":229689131,\"uploadInfo\":{\"fileName\":\"Microsoft.GamePackage_1.1.1.0_x64_abcdefghij_erasymbol.zip\",\"xfusId\":\"12345678-1234-1234-1234-1234567890ab\",\"fileSasUri\":\"https://bloburl.blob.core.windows.net/12345678-1234-1234-1234-1234567890ab/Microsoft.GamePackage_1.1.1.0_x64_abcdefghij_erasymbol.zip?REDACTED\",\"xfusTenant\":\"ZZZ\"},\"id\":\"EraSymbolFile\"}";

        var sanitizedResponse = LogSanitizer.SanitizeJsonResponse(response);

        Assert.AreEqual(expectedSanitizedResponse, sanitizedResponse);
    }

}