// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace GameStoreBroker.ClientApi.Client.Ingestion.Models
{
    public class GameSubmission
    {
        public string Id { get; internal init; }

        public GameSubmissionState GameSubmissionState { get; internal init; }

        public int ReleaseNumber { get; internal init; }

        public string FriendlyName { get; internal init; }

        public DateTime? PublishedTimeInUtc { get; internal init; }

        public List<GameSubmissionValidationItem> SubmissionValidationItems { get; internal set; }
    }
}
