// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using PackageUploader.ClientApi.Client.Ingestion.Models;

namespace PackageUploader.ClientApi.Models
{
    public class GamePublishConfiguration
    {
        private DateTime? _releaseTime;

        /// <summary>
        /// Scheduled release time (UTC). Default value is null, and submission will be published as soon as possible.
        /// </summary>
        public DateTime? ReleaseTime
        {
            get => _releaseTime;
            set => _releaseTime = GetUtcDateWithHour(value);
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
                ReleaseTimeInUtc = IsManualPublish ? null : ReleaseTime,
            };

        private static DateTime? GetUtcDateWithHour(DateTime? dateTime)
        {
            if (!dateTime.HasValue)
                return null;

            var input = dateTime.Value.ToUniversalTime();
            var output = new DateTime(input.Year, input.Month, input.Day, input.Hour, 0, 0, DateTimeKind.Utc);
            return output;
        }
    }
}
