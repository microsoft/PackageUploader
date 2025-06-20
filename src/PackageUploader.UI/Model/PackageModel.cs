// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Windows.Media.Imaging;

namespace PackageUploader.UI.Model;

public class PackageModel
{
    public string BigId { get; set; } = string.Empty;
    public string TitleId { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string PackageFilePath { get; set; } = string.Empty;
    public string EkbFilePath { get; set; } = string.Empty;
    public string SubValFilePath { get; set; } = string.Empty;
    public string SymbolBundleFilePath { get; internal set; } = string.Empty;
    public string GameConfigFilePath { get; set; } = string.Empty;
    public string PackageSize { get; set; } = string.Empty;
    public string PackageType { get; set; } = string.Empty;
    public BitmapImage? PackagePreviewImage { get; set; } = null;
    public string BranchId { get; internal set; } = string.Empty;
}
