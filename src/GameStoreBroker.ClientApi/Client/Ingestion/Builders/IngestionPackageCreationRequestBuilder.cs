// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using PackageUploader.ClientApi.Client.Ingestion.Models.Internal;

namespace PackageUploader.ClientApi.Client.Ingestion.Builders
{
    internal class IngestionPackageCreationRequestBuilder : IBuilder<IngestionPackageCreationRequest>
    {
        private readonly string _currentDraftInstanceId;
        private readonly string _fileName;
        private readonly string _marketGroupId;
        private const string ResourceType = "PackageCreationRequest";

        public IngestionPackageCreationRequestBuilder(string currentDraftInstanceId, string fileName, string marketGroupId)
        {
            _currentDraftInstanceId = currentDraftInstanceId ?? throw new ArgumentNullException(nameof(currentDraftInstanceId));
            _fileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
            _marketGroupId = marketGroupId ?? throw new ArgumentNullException(nameof(marketGroupId));
        }

        public IngestionPackageCreationRequest Build() =>
            new ()
            {
                PackageConfigurationId = _currentDraftInstanceId,
                FileName = _fileName,
                ResourceType = ResourceType,
                MarketGroupId = _marketGroupId,
            };
    }
}