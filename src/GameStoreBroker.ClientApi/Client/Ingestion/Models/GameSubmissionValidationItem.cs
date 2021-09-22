// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace GameStoreBroker.ClientApi.Client.Ingestion.Models
{
    public sealed class GameSubmissionValidationItem
    {
        public string ErrorCode { get; internal init; }

        /// <summary>
        /// Severity for validation [Informational, Warning, Error]
        /// </summary>
        public string Severity { get; internal init; }

        public string Message { get; internal init; }

        public string Resource { get; internal init; }
    }
}
