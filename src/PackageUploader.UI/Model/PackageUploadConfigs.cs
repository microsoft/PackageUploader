// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace PackageUploader.UI.Model;

#pragma warning disable IDE1006 // Naming Styles. Beginning with a lowercase letter matches the JSON schema for PackageUploader.

internal class UploadConfig
{
    public string operationName { get; } = "UploadXvcPackage";
    public AadAuthInfo? aadAuthInfo { get; set; }
    public string? productId { get; set; }
    public string bigId { get; set; } = string.Empty;
    public string branchFriendlyName { get; set; } = "Main";
    public string? flightName { get; set; }
    public string marketGroupName { get; set; } = "default";
    public string packageFilePath { get; set; } = string.Empty;
    public bool deltaUpload { get; set; } = true;
    public GameAssets gameAssets { get; set; } = new GameAssets();
    public int minutesToWaitForProcessing { get; set; } = 60;
    public AvailabilityDate? availabilityDate { get; set; }
    public AvailabilityDate? preDownloadDate { get; set; }
    public UploadClientConfig uploadConfig { get; set; } = new UploadClientConfig();
}

internal class AadAuthInfo
{
    public string tenantId { get; set; } = string.Empty;
    public string clientId { get; set; } = string.Empty;
    public string certificateThumbprint { get; set; } = string.Empty;
    public string? certificateStore { get; set; }
    public string? certificateLocation { get; set; }
}

internal class GameAssets
{
    public string ekbFilePath { get; set; } = string.Empty;
    public string subValFilePath { get; set; } = string.Empty;
    public string symbolsFilePath { get; set; } = string.Empty;
    public string discLayoutFilePath { get; set; } = string.Empty;
}

internal class AvailabilityDate
{
    public bool? isEnabled { get; set; } = false;
    public DateTime? effectiveDate { get; set; }
}

internal class UploadClientConfig
{
    public int httpTimeoutMs { get; set; } = 300000;
    public int httpUploadTimeoutMs { get; set; } = 300000;
    public int maxParallelism { get; set; } = 24;
    public int defaultConnectionLimit { get; set; } = -1;
    public bool expect100Continue { get; set; } = false;
    public bool useNagleAlgorithm { get; set; } = false;
}

internal class GetProductConfig
{
    public string operationName { get; } = "GetProduct";
    public AadAuthInfo? aadAuthInfo { get; set; }
    public string? productId { get; set; }
    public string bigId { get; set; } = string.Empty;
}
internal class GetProductResponse
{
    public string productId { get; set; } = string.Empty;
    public string productName { get; set; } = string.Empty;
    public string bigId { get; set; } = string.Empty;
    public string[] branchFriendlyNames { get; set; } = [];
    public string[] flightNames { get; set; } = [];
}

#pragma warning restore IDE1006 // Naming Styles

