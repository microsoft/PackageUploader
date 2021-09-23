// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GameStoreBroker.ClientApi.Client.Ingestion.Models.Internal;
using System;
using System.Collections.Generic;

namespace GameStoreBroker.ClientApi.Client.Ingestion.Builders
{
    internal class IngestionSubmissionCreationRequestBuilder : IBuilder<IngestionSubmissionCreationRequest>
    {
        private readonly string _currentDraftInstanceId;
        private readonly string _destinationSandboxName;
        private const string ResourceType = "SubmissionCreationRequest";

        public IngestionSubmissionCreationRequestBuilder(string currentDraftInstanceId, string destinationSandboxName)
        {
            _currentDraftInstanceId = currentDraftInstanceId ?? throw new ArgumentNullException(nameof(currentDraftInstanceId));
            _destinationSandboxName = destinationSandboxName ?? throw new ArgumentNullException(nameof(destinationSandboxName));
        }

        public IngestionSubmissionCreationRequest Build() =>
            new()
            {
                ResourceType = ResourceType,
                Targets = new List<TypeValuePair>
                {
                    new()
                    {
                        Type = IngestionSubmissionTargetType.Sandbox.ToString(),
                        Value = _destinationSandboxName,
                    }
                },
                Resources = new List<TypeValuePair>
                {
                    new()
                    {
                        Type = IngestionBranchModuleType.Package.ToString(),
                        Value = _currentDraftInstanceId,
                    }
                }
            };
    }
}