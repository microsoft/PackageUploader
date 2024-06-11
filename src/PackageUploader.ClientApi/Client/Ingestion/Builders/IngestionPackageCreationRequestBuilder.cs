// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using PackageUploader.ClientApi.Client.Ingestion.Models;
using PackageUploader.ClientApi.Client.Ingestion.Models.Internal;

namespace PackageUploader.ClientApi.Client.Ingestion.Builders;

internal class IngestionPackageCreationRequestBuilder : IBuilder<IngestionPackageCreationRequest>
{
    private readonly string _currentDraftInstanceId;
    private readonly string _fileName;
    private readonly string _marketGroupId;
    private readonly ClientExtractedMetaData _clientExtractedMetaData;
    private const string ResourceType = "PackageCreationRequest";

    public IngestionPackageCreationRequestBuilder(string currentDraftInstanceId, string fileName, string marketGroupId, bool ixXvc, XvcTargetPlatform xvcTargetPlatform)
    {
        _currentDraftInstanceId = currentDraftInstanceId ?? throw new ArgumentNullException(nameof(currentDraftInstanceId));
        _fileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
        _marketGroupId = marketGroupId ?? throw new ArgumentNullException(nameof(marketGroupId));

        if (ixXvc)
        {
            _clientExtractedMetaData = CreateClientExtractedMetaData(xvcTargetPlatform);
        }
    }

    public IngestionPackageCreationRequest Build() =>
        new ()
        {
            PackageConfigurationId = _currentDraftInstanceId,
            FileName = _fileName,
            ResourceType = ResourceType,
            MarketGroupId = _marketGroupId,
            ClientExtractedMetaData = _clientExtractedMetaData,
        };

    private static ClientExtractedMetaData CreateClientExtractedMetaData(XvcTargetPlatform xvcTargetPlatform)
    {
        var xvcReader = new XvcReader
        {
            XvcTargetPlatform = xvcTargetPlatform,
            GameConfig = string.Empty,
        };

        var clientExtractedMetaData = new ClientExtractedMetaData
        {
            XvcReader = xvcReader,
        };

        return clientExtractedMetaData;
    }
}