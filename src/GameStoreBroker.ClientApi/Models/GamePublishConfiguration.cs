// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using GameStoreBroker.ClientApi.Client.Ingestion.Models;

namespace GameStoreBroker.ClientApi.Models
{
    public class GamePublishConfiguration
    {
        private DateTime? _releaseTimeInUtc;

        /// <summary>
        /// Scheduled release time (UTC). Default value is null, and submission will be published as soon as possible.
        /// </summary>
        public DateTime? ReleaseTimeInUtc
        {
            get => _releaseTimeInUtc;
            set => _releaseTimeInUtc = value?.ToUniversalTime();
        }

        /// <summary>
        /// Flag of if manual publish is enabled. Default value is false.
        /// </summary>
        public bool IsManualPublish { get; set; }

        /// <summary>
        /// Certification notes
        /// </summary>
        public string CertificationNotes { get; set; }

        internal GameSubmissionOptions ToGameSubmissionOptions() =>
            new()
            {
                CertificationNotes = CertificationNotes,
                IsManualPublish = IsManualPublish,
                ReleaseTimeInUtc = IsManualPublish ? null : ReleaseTimeInUtc,
            };
    }
}
