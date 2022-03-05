// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.ClientApi.Client.Ingestion.Mappers;
using PackageUploader.ClientApi.Client.Ingestion.Models;
using PackageUploader.ClientApi.Client.Ingestion.Models.Internal;
using System;
using System.Collections.Generic;

namespace PackageUploader.ClientApi.Client.Ingestion.Builders;

internal class IngestionSubmissionCreationRequestBuilder : IBuilder<IngestionSubmissionCreationRequest>
{
    private readonly string _currentDraftInstanceId;
    private readonly string _target;
    private readonly IngestionSubmissionTargetType _targetType;
    private readonly GameSubmissionOptions _options;
    private const string ResourceType = "SubmissionCreationRequest";

    public IngestionSubmissionCreationRequestBuilder(string currentDraftInstanceId, string target, IngestionSubmissionTargetType targetType, GameSubmissionOptions options = null)
    {
        _currentDraftInstanceId = currentDraftInstanceId ?? throw new ArgumentNullException(nameof(currentDraftInstanceId));
        _target = target ?? throw new ArgumentNullException(nameof(target));
        _targetType = targetType;
        _options = options;
    }

    public IngestionSubmissionCreationRequest Build() =>
        new()
        {
            ResourceType = ResourceType,
            Targets = new List<TypeValuePair>
            {
                new()
                {
                    Type = _targetType.ToString(),
                    Value = _target,
                }
            },
            Resources = new List<TypeValuePair>
            {
                new()
                {
                    Type = IngestionBranchModuleType.Package.ToString(),
                    Value = _currentDraftInstanceId,
                }
            },
            PublishOption = _options.Map(),
        };
}