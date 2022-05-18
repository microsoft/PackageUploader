// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace PackageUploader.ClientApi.Client.Ingestion.Models.Internal;

public class ClientExtractedMetaData
{
    public XvcReader XvcReader { get; set; }
}

public class XvcReader
{
    public XvcTargetPlatform XvcTargetPlatform { get; set; }
    public string GameConfig { get; set; }
}