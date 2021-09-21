// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GameStoreBroker.ClientApi.Client.Ingestion.Models.Internal;

namespace GameStoreBroker.ClientApi.Client.Ingestion.Builders
{
    internal class IngestionPackageCreationRequestBuilder
    {
        private readonly string _currentDraftInstanceId;
        private readonly string _fileName;
        private readonly string _marketGroupId;
        private const string ResourceType = "PackageCreationRequest";

        public IngestionPackageCreationRequestBuilder(string currentDraftInstanceId, string fileName, string marketGroupId)
        {
            _currentDraftInstanceId = currentDraftInstanceId;
            _fileName = fileName;
            _marketGroupId = marketGroupId;
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