// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace PackageUploader.ClientApi.Models
{
    public class GamePackageDate
    {
        public bool IsEnabled { get; set; }

        private DateTime? _effectiveDate;
        public DateTime? EffectiveDate
        {
            get => _effectiveDate;
            set => _effectiveDate = GetUtcDateWithHour(value);
        }

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