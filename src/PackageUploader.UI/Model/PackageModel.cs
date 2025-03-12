// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace PackageUploader.UI.Model;

public class PackageModel
{
    public string BigId { get; set; } = string.Empty;
    public string PackageFilePath { get; set; } = string.Empty;
    public string EkbFilePath { get; set; } = string.Empty;
    public string SubValFilePath { get; set; } = string.Empty;
    public string SymbolBundleFilePath { get; internal set; } = string.Empty;
}
