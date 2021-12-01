// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace PackageUploader.ClientApi.Client.Ingestion.Models.Internal
{
    internal sealed class IngestionPublishOption
    {
        /// <summary>
        /// Scheduled release time (UTC). Default value is null, and submission will be published as soon as possible.
        /// </summary>
        public DateTime? ReleaseTimeInUtc { get; set; }

        /// <summary>
        /// Flag of if manual publish is enabled. Default value is false.
        /// </summary>
        public bool IsManualPublish { get; set; }

        /// <summary>
        /// Flag of if auto promotion is enabled. Default value is false.
        /// </summary>
        public bool IsAutoPromote { get; set; }

        /// <summary>
        /// Certification notes
        /// </summary>
        public string CertificationNotes { get; set; }
    }
}
