// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace PackageUploader.ClientApi.Client.Ingestion.Models.Internal
{
    internal sealed class IngestionSubmissionValidationItem
    {
        /// <summary>
        /// Validation error code
        /// </summary>
        public string ErrorCode { get; set; }

        /// <summary>
        /// Severity for validation [Informational, Warning, Error]
        /// </summary>
        public string Severity { get; set; }

        /// <summary>
        /// Message for validation
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Resource related to validation item
        /// </summary>
        public string Resource { get; set; }
    }
}
